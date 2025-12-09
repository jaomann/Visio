using OpenCvSharp;
using System.Diagnostics;

namespace Visio.Services.Implementations;

public class ImageProcessingService : Visio.Services.Interfaces.IImageProcessingService
{
    private CascadeClassifier? _faceCascade;

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
        if (_faceCascade == null) 
            return input.Clone();

        var output = input.Clone();
        var gray = new Mat();

        Cv2.CvtColor(input, gray, ColorConversionCodes.BGR2GRAY);
        Cv2.EqualizeHist(gray, gray);

        var faces = _faceCascade.DetectMultiScale(
            gray,
            scaleFactor: 1.05,
            minNeighbors: 3,
            flags: HaarDetectionTypes.ScaleImage,
            minSize: new OpenCvSharp.Size(20, 20),
            maxSize: new OpenCvSharp.Size(500, 500)
        );

        foreach (var face in faces)
        {
            Cv2.Rectangle(output, face, Scalar.Green, 3);
            
            var centerX = face.X + face.Width / 2;
            var centerY = face.Y + face.Height / 2;
            Cv2.Circle(output, new OpenCvSharp.Point(centerX, centerY), 3, Scalar.Red, -1);
        }

        gray.Dispose();
        return output;
    }

    private void InitializeFaceDetection()
    {
        var xmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "haarcascade_frontalface_default.xml");
        
        if (File.Exists(xmlPath))
        {
            _faceCascade = new CascadeClassifier(xmlPath);
            Debug.WriteLine("[ImageProcessing] Haar Cascade carregado com sucesso");
        }
        else
        {
            Debug.WriteLine($"[ImageProcessing] Haar Cascade não encontrado: {xmlPath}");
        }
    }
}
