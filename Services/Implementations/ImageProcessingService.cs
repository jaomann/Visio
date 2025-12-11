using OpenCvSharp;
using System.Diagnostics;

namespace Visio.Services.Implementations;

public class ImageProcessingService : Visio.Services.Interfaces.IImageProcessingService
{
    private CascadeClassifier? _faceCascade;
    private List<OpenCvSharp.Rect> _previousFaces = new List<OpenCvSharp.Rect>();
    private int _framesSinceLastDetection = 0;
    private const int MAX_FRAMES_WITHOUT_DETECTION = 5;

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
        if (_faceCascade == null || _faceCascade.Empty())
            return input.Clone();

        var output = input.Clone();
        var gray = new Mat();

        Cv2.CvtColor(input, gray, ColorConversionCodes.BGR2GRAY);
        Cv2.EqualizeHist(gray, gray);

        var faces = _faceCascade.DetectMultiScale(
            gray,
            scaleFactor: 1.1,
            minNeighbors: 3,
            flags: HaarDetectionTypes.ScaleImage,
            minSize: new OpenCvSharp.Size(50, 50),
            maxSize: new OpenCvSharp.Size(500, 500)
        );

        var validFaces = new List<OpenCvSharp.Rect>();
        
        foreach (var face in faces)
        {
            float aspectRatio = (float)face.Width / face.Height;
            
            if (aspectRatio >= 0.75f && aspectRatio <= 1.2f &&
                face.Width >= 50 && face.Height >= 50 &&
                HasSufficientVariance(gray, face))
            {
                validFaces.Add(face);
            }
        }

        var trackedFaces = MergeFacesWithTracking(validFaces);

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
                    var smoothedFace = new OpenCvSharp.Rect(
                        (currentFace.X + matchingPrevFace.X) / 2,
                        (currentFace.Y + matchingPrevFace.Y) / 2,
                        (currentFace.Width + matchingPrevFace.Width) / 2,
                        (currentFace.Height + matchingPrevFace.Height) / 2
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
            return stddev.Val0 > 12.0;
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
        
        return distance < 50 && sizeDiff < 30;
    }

    private void InitializeFaceDetection()
    {
        try
        {
            var xmlPath = Path.Combine(FileSystem.AppDataDirectory, "haarcascade_frontalface_default.xml");
            
            if (!File.Exists(xmlPath))
            {
                using var stream = FileSystem.OpenAppPackageFileAsync("haarcascade_frontalface_default.xml").Result;
                using var fileStream = File.Create(xmlPath);
                stream.CopyTo(fileStream);
            }
            
            _faceCascade = new CascadeClassifier(xmlPath);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ImageProcessing] Erro ao carregar Haar Cascade: {ex.Message}");
        }
    }
}
