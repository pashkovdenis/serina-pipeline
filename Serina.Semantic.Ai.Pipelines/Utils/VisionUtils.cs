namespace Serina.Semantic.Ai.Pipelines.Utils
{
    public static class VisionUtils
    {
        public static string ConvertImageToDataUri(string imagePath)
        {
            // Read the image as a byte array
            byte[] imageBytes = File.ReadAllBytes(imagePath);

            // Determine the MIME type (e.g., "image/jpeg")
            string mimeType = GetMimeType(imagePath);

            // Convert the byte array to a Base64 string
            string base64String = Convert.ToBase64String(imageBytes);

            // Return the Base64 string with the "data:" prefix
            return $"data:{mimeType};base64,{base64String}";
        }

        public static string GetMimeType(string filePath)
        {
            // Simple extension-based MIME type detection
            string extension = Path.GetExtension(filePath).ToLowerInvariant();
            return extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".webp" => "image/webp",
                _ => "application/octet-stream", // Fallback for unknown types
            };
        }
         
    }
}
