using OpenCvSharp;

namespace Visio.Services.Interfaces;

public interface IImageProcessingService
{
    Mat ApplyGrayscale(Mat input);
    Mat ApplyBlur(Mat input);
    Mat ApplyEdgeDetection(Mat input);
    Mat ApplyFaceDetection(Mat input);
}
