using OpenCvSharp;
using System.Diagnostics;

namespace Visio.Services.Implementations;

public class ImageProcessingService : Visio.Services.Interfaces.IImageProcessingService
{
    private CascadeClassifier? _faceCascadeAlt2;
    private CascadeClassifier? _faceCascadeDefault;
    private CascadeClassifier? _faceCascadeProfile;
    
    private List<OpenCvSharp.Rect> _previousFaces = new List<OpenCvSharp.Rect>();
    private int _framesSinceLastDetection = 0;
    private const int MAX_FRAMES_WITHOUT_DETECTION = 5;
    
    private readonly Queue<OpenCvSharp.Rect> _faceBuffer = new Queue<OpenCvSharp.Rect>();
    private const int BUFFER_SIZE = 5;

    public ImageProcessingService()
    {
        InitializeFaceDetection();
    }

    public Mat ApplyGrayscale(Mat input)
    {
        var gray = new Mat();
        var output = new Mat();
        
        Cv2.CvtColor(input, gray, ColorConversionCodes.BGR2GRAY);
        Cv2.CvtColor(gray, output, ColorConversionCodes.GRAY2BGR);
        
        gray.Dispose();
        return output;
    }

    public Mat ApplyBlur(Mat input)
    {
        var output = new Mat();
        Cv2.GaussianBlur(input, output, new OpenCvSharp.Size(15, 15), 0);
        return output;
    }

    public Mat ApplyEdgeDetection(Mat input)
    {
        var gray = new Mat();
        var edges = new Mat();
        var output = new Mat();

        Cv2.CvtColor(input, gray, ColorConversionCodes.BGR2GRAY);
        Cv2.Canny(gray, edges, 50, 150);
        Cv2.CvtColor(edges, output, ColorConversionCodes.GRAY2BGR);

        gray.Dispose();
        edges.Dispose();

        return output;
    }

    public Mat ApplyFaceDetection(Mat input)
    {
        if (_faceCascadeAlt2 == null || _faceCascadeAlt2.Empty())
            return input.Clone();

        var output = input.Clone();
        var gray = new Mat();

        Cv2.CvtColor(input, gray, ColorConversionCodes.BGR2GRAY);

        // Apply CLAHE only if scene is dark
        double brightness = Cv2.Mean(gray).Val0;
        if (brightness < 80)
        {
            using var clahe = Cv2.CreateCLAHE(clipLimit: 3.0, tileGridSize: new OpenCvSharp.Size(8, 8));
            clahe.Apply(gray, gray);
        }

        // Dynamic minSize based on resolution
        var dynamicMinSize = new OpenCvSharp.Size(input.Width / 15, input.Height / 15);
        
        var faces = _faceCascadeAlt2.DetectMultiScale(
            gray,
            scaleFactor: 1.05,
            minNeighbors: 6,
            flags: HaarDetectionTypes.ScaleImage,
            minSize: dynamicMinSize,
            maxSize: new OpenCvSharp.Size(input.Width / 2, input.Height / 2)
        );

        // Fallback to default cascade if alt2 found nothing
        if (faces.Length == 0 && _faceCascadeDefault != null && !_faceCascadeDefault.Empty())
        {
            faces = _faceCascadeDefault.DetectMultiScale(
                gray,
                scaleFactor: 1.05,
                minNeighbors: 6,
                flags: HaarDetectionTypes.ScaleImage,
                minSize: dynamicMinSize,
                maxSize: new OpenCvSharp.Size(input.Width / 2, input.Height / 2)
            );
        }

        // Fallback to profile cascade for side faces
        if (faces.Length == 0 && _faceCascadeProfile != null && !_faceCascadeProfile.Empty())
        {
            faces = _faceCascadeProfile.DetectMultiScale(
                gray,
                scaleFactor: 1.05,
                minNeighbors: 6,
                flags: HaarDetectionTypes.ScaleImage,
                minSize: dynamicMinSize,
                maxSize: new OpenCvSharp.Size(input.Width / 2, input.Height / 2)
            );
        }

        var validFaces = new List<OpenCvSharp.Rect>();
        
        foreach (var face in faces)
        {
            float aspectRatio = (float)face.Width / face.Height;
            
            if (aspectRatio < 0.6f || aspectRatio > 1.4f)
                continue;

            if (face.Width < dynamicMinSize.Width || face.Height < dynamicMinSize.Height)
                continue;

            if (!HasSufficientVariance(gray, face))
                continue;

            validFaces.Add(face);
        }

        // Temporal buffering - add to buffer
        if (validFaces.Count > 0)
        {
            _faceBuffer.Enqueue(validFaces[0]);
            if (_faceBuffer.Count > BUFFER_SIZE)
                _faceBuffer.Dequeue();
        }

        // Calculate average from buffer
        OpenCvSharp.Rect? finalFace = null;
        if (_faceBuffer.Count > 0)
        {
            var avgX = (int)_faceBuffer.Average(f => f.X);
            var avgY = (int)_faceBuffer.Average(f => f.Y);
            var avgW = (int)_faceBuffer.Average(f => f.Width);
            var avgH = (int)_faceBuffer.Average(f => f.Height);
            finalFace = new OpenCvSharp.Rect(avgX, avgY, avgW, avgH);
        }

        var trackedFaces = finalFace.HasValue ? new List<OpenCvSharp.Rect> { finalFace.Value } : new List<OpenCvSharp.Rect>();
        trackedFaces = MergeFacesWithTracking(trackedFaces);

        foreach (var face in trackedFaces)
        {
            Cv2.Rectangle(output, face, Scalar.LimeGreen, 2);
            
            var centerX = face.X + face.Width / 2;
            var centerY = face.Y + face.Height / 2;
            Cv2.Circle(output, new OpenCvSharp.Point(centerX, centerY), 3, Scalar.Red, -1);
            
            Cv2.PutText(
                output,
                $"Face - {face.Width}:{face.Height}",
                new OpenCvSharp.Point(face.X, face.Y - 10),
                HersheyFonts.HersheySimplex,
                0.5,
                Scalar.LimeGreen,
                2
            );
        }

        gray.Dispose();
        return output;
    }

    private List<OpenCvSharp.Rect> MergeFacesWithTracking(List<OpenCvSharp.Rect> currentFaces)
    {
        var result = new List<OpenCvSharp.Rect>();

        if (currentFaces.Count > 0)
        {
            _framesSinceLastDetection = 0;
            
            foreach (var currentFace in currentFaces)
            {
                var matchingPrevFace = _previousFaces.FirstOrDefault(prevFace => 
                    IsSameFace(currentFace, prevFace));

                if (matchingPrevFace != default(OpenCvSharp.Rect))
                {
                    // Exponential smoothing
                    var smoothedFace = new OpenCvSharp.Rect(
                        (int)(matchingPrevFace.X * 0.7 + currentFace.X * 0.3),
                        (int)(matchingPrevFace.Y * 0.7 + currentFace.Y * 0.3),
                        (int)(matchingPrevFace.Width * 0.7 + currentFace.Width * 0.3),
                        (int)(matchingPrevFace.Height * 0.7 + currentFace.Height * 0.3)
                    );
                    result.Add(smoothedFace);
                }
                else
                {
                    result.Add(currentFace);
                }
            }
            
            _previousFaces = new List<OpenCvSharp.Rect>(result);
        }
        else
        {
            _framesSinceLastDetection++;
            
            if (_framesSinceLastDetection < MAX_FRAMES_WITHOUT_DETECTION && _previousFaces.Count > 0)
            {
                result = new List<OpenCvSharp.Rect>(_previousFaces);
            }
            else
            {
                _previousFaces.Clear();
            }
        }

        return result;
    }

    private bool HasSufficientVariance(Mat grayImage, OpenCvSharp.Rect region)
    {
        try
        {
            using var roi = new Mat(grayImage, region);
            Cv2.MeanStdDev(roi, out var mean, out var stddev);
            return stddev.Val0 > 8.0;
        }
        catch
        {
            return true;
        }
    }

    private bool IsSameFace(OpenCvSharp.Rect face1, OpenCvSharp.Rect face2)
    {
        var center1X = face1.X + face1.Width / 2;
        var center1Y = face1.Y + face1.Height / 2;
        var center2X = face2.X + face2.Width / 2;
        var center2Y = face2.Y + face2.Height / 2;
        
        var distance = Math.Sqrt(Math.Pow(center1X - center2X, 2) + Math.Pow(center1Y - center2Y, 2));
        var sizeDiff = Math.Abs(face1.Width - face2.Width) + Math.Abs(face1.Height - face2.Height);
        
        // Adaptive matching based on actual face size
        return distance < Math.Max(face1.Width, face1.Height) * 0.8 &&
               sizeDiff < face1.Width * 0.5;
    }

    private void InitializeFaceDetection()
    {
        try
        {
            Debug.WriteLine("[ImageProcessing] === Iniciando carregamento dos Haar Cascades ===");
            
            LoadCascade("haarcascade_frontalface_alt2.xml", ref _faceCascadeAlt2, "Alt2");
            LoadCascade("haarcascade_frontalface_default.xml", ref _faceCascadeDefault, "Default");
            LoadCascade("haarcascade_profileface.xml", ref _faceCascadeProfile, "Profile");
            
            Debug.WriteLine("[ImageProcessing] === Carregamento concluído ===");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ImageProcessing] ERRO CRITICO: {ex.Message}");
            Debug.WriteLine($"[ImageProcessing] Stack trace: {ex.StackTrace}");
        }
    }

    private void LoadCascade(string filename, ref CascadeClassifier? cascade, string name)
    {
        try
        {
            var xmlPath = Path.Combine(FileSystem.AppDataDirectory, filename);
            Debug.WriteLine($"[ImageProcessing] Carregando {name}: {xmlPath}");
            
            if (!File.Exists(xmlPath))
            {
                Debug.WriteLine($"[ImageProcessing] {name} não encontrado, copiando do pacote...");
                try
                {
                    using var stream = FileSystem.OpenAppPackageFileAsync(filename).Result;
                    Debug.WriteLine($"[ImageProcessing] Stream aberto, tamanho: {stream.Length} bytes");
                    
                    using var fileStream = File.Create(xmlPath);
                    stream.CopyTo(fileStream);
                    Debug.WriteLine($"[ImageProcessing] {name} copiado com sucesso!");
                }
                catch (Exception copyEx)
                {
                    Debug.WriteLine($"[ImageProcessing] {name} nao disponivel: {copyEx.Message}");
                    return;
                }
            }
            
            cascade = new CascadeClassifier(xmlPath);
            
            if (cascade == null || cascade.Empty())
            {
                Debug.WriteLine($"[ImageProcessing] ERRO: {name} esta vazio ou nulo!");
            }
            else
            {
                Debug.WriteLine($"[ImageProcessing] {name} carregado com sucesso!");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ImageProcessing] Erro ao carregar {name}: {ex.Message}");
        }
    }
}
