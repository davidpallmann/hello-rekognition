using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.Rekognition;
using Amazon.Rekognition.Model;
using System.Drawing;
using System.Drawing.Imaging;

namespace hello_rekognition
{
#pragma warning disable CA1416

    public class RekognitionHelper
    {
        private string _bucketname { get; set; }
        private RegionEndpoint _region { get; set; }
        private AmazonRekognitionClient _rekognitionClient { get; set; }
        private AmazonS3Client _s3Client { get; set; }

        public RekognitionHelper(string bucketname, RegionEndpoint region)
        {
            _bucketname = bucketname;
            _region = region;
            _rekognitionClient = new AmazonRekognitionClient(_region);
            _s3Client = new AmazonS3Client(_region);
        }

        /// <summary>
        /// Detect labels.
        /// </summary>
        /// <param name="filename"></param>

        public async Task DetectLabels(string filename)
        {
            var image = await UploadFileToBucket(filename);

            DetectLabelsRequest detectlabelsRequest = new DetectLabelsRequest()
            {
                Image = image,
                MaxLabels = 10,
                MinConfidence = 75F
            };

            var detectLabelsResponse = await _rekognitionClient.DetectLabelsAsync(detectlabelsRequest);
            Console.WriteLine("Detected labels for " + filename);
            foreach (var label in detectLabelsResponse.Labels)
                Console.WriteLine($"{label.Name}, {label.Confidence}");

            await DeleteFileFromBucket(filename);
        }

        /// <summary>
        /// Detect moderation labels.
        /// </summary>
        /// <param name="filename"></param>

        public async Task DetectModerationLabels(string filename)
        {
            var image = await UploadFileToBucket(filename);

            var detectModerationLabelsRequest = new DetectModerationLabelsRequest()
            {
                Image = image,
                MinConfidence = 75F
            };

            var detectModerationLabelsResponse = await _rekognitionClient.DetectModerationLabelsAsync(detectModerationLabelsRequest);
            Console.WriteLine("Detected labels for " + filename);
            foreach (var label in detectModerationLabelsResponse.ModerationLabels)
            {
                Console.WriteLine($"{label.Name}, {label.Confidence}");
            }

            await DeleteFileFromBucket(filename);
        }

        /// <summary>
        /// Recognize celebrities.
        /// </summary>
        /// <param name="filename"></param>

        public async Task RecognizeCelebrities(string filename)
        {
            var image = await UploadFileToBucket(filename);

            var recognizeCelebritiesRequest = new RecognizeCelebritiesRequest()
            {
                Image = image
            };

            var recognizeCelebritiesResponse = await _rekognitionClient.RecognizeCelebritiesAsync(recognizeCelebritiesRequest);
            Console.WriteLine("Detected celebrities for " + filename);
            foreach (var celebrity in recognizeCelebritiesResponse.CelebrityFaces)
            {
                Console.WriteLine($"{celebrity.Name}, {celebrity.MatchConfidence}");
            }

            await DeleteFileFromBucket(filename);
        }

        /// <summary>
        /// Detect faces. Create a copy of original image with faces highlighted.
        /// </summary>
        /// <param name="filename"></param>

        public async Task DetectFaces(string filename)
        {
            var image = await UploadFileToBucket(filename);

            var detectFacesRequest = new DetectFacesRequest
            {
                Attributes = new List<String>() { "ALL" },
                Image = image
            };

            var detectFacesResponse = await _rekognitionClient.DetectFacesAsync(detectFacesRequest);
            Console.WriteLine($"Detected {detectFacesResponse.FaceDetails.Count} face(s) in " + filename);

            // create a duplicate image with the faces highlighted

            Pen pen = new Pen(Brushes.SkyBlue, 3);
            var facesHighlighted = System.Drawing.Image.FromFile(filename);
            using (var graphics = Graphics.FromImage(facesHighlighted))
            {
                foreach(var face in detectFacesResponse.FaceDetails)
                {
                    BoundingBox bb = face.BoundingBox;
                    Console.WriteLine($"  Face found at location ({bb.Top}, {bb.Left}) {bb.Height} x {bb.Width}");
                    Console.WriteLine($"    Gender: {face.Gender.Value}, Age range: {face.AgeRange.Low}-{face.AgeRange.High}, Smiling: {face.Smile.Value}, Eyeglasses: {face.Eyeglasses.Value}, Confidence: {face.Confidence}");
                    graphics.DrawRectangle(pen, x: facesHighlighted.Width * bb.Left, y: facesHighlighted.Height * bb.Top, 
                        width: facesHighlighted.Width * bb.Width, height: facesHighlighted.Height * bb.Height);
                }
            }

            // Save the image with highlights as a jpeg file

            var filenameFacesHighlighted = filename.Replace(Path.GetExtension(filename), "_Faces.jpg");
            facesHighlighted.Save(filenameFacesHighlighted, ImageFormat.Jpeg);
            Console.WriteLine($"Generated image file {filenameFacesHighlighted} with {detectFacesResponse.FaceDetails.Count} face(s) highlighted.");
            await DeleteFileFromBucket(filename);
        }

        /// <summary>
        /// Upload local file to S3 bucket.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns>Amazon Rekognition Image object.</returns>
            private async Task<Amazon.Rekognition.Model.Image> UploadFileToBucket(string filename)
        {
            Console.WriteLine($"Upload {filename} to bucket {_bucketname}");
            var putRequest = new PutObjectRequest
            {
                BucketName = _bucketname,
                FilePath = filename,
                Key = Path.GetFileName(filename)
            };
            await _s3Client.PutObjectAsync(putRequest);

            return new Amazon.Rekognition.Model.Image
            {
                S3Object = new Amazon.Rekognition.Model.S3Object
                {
                    Bucket = _bucketname,
                    Name = putRequest.Key
                }
            };
        }

        /// <summary>
        /// Delete file from S3 bucket.
        /// </summary>
        /// <param name="filename"></param>
        private async Task DeleteFileFromBucket(string filename)
        {
            Console.WriteLine($"Delete {filename} from bucket {_bucketname}");
            var deleteRequest = new DeleteObjectRequest
            {
                BucketName = _bucketname,
                Key = Path.GetFileName(filename)
            };
            await _s3Client.DeleteObjectAsync(deleteRequest);
        }
    }
}