using Amazon;
using hello_rekognition;

const string bucketname = @"hello-rekog";
var region = RegionEndpoint.USWest1;

if (args.Length == 2)
{
    var filename = args[0];
    var analysisType = (args.Length > 1) ? args[1] : "text";

    try
    { 
        RekognitionHelper helper = new RekognitionHelper(bucketname, region);

        switch (analysisType)
        {
            case "labels":
                await helper.DetectLabels(filename);
                Environment.Exit(1);
                break;
            case "moderate":
                await helper.DetectModerationLabels(filename);
                Environment.Exit(1);
                break;
            case "celebrity":
                await helper.RecognizeCelebrities(filename);
                Environment.Exit(1);
                break;
            case "faces":
                await helper.DetectFaces(filename);
                Environment.Exit(1);
                break;
        }
    }
    catch (Exception e)
    {
        Console.WriteLine($"EXCEPTION {e.Message}");
    }
}

Console.WriteLine("?Invalid parameter - command line format: dotnet run -- <file> labels|moderate|celebrity|faces");
