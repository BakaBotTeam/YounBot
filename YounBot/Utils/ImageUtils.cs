namespace YounBot.Utils;

public static class ImageUtils
{
    
    public static async Task<byte[]> UrlToImageMessageAsync(string url)
    {
        using HttpClient httpClient = new();
        using HttpResponseMessage response = await httpClient.GetAsync(url);
        
        if (response.IsSuccessStatusCode) 
        {
            byte[] image = ReadStreamToByteArray(await response.Content.ReadAsStreamAsync());
            return image;
        }
        
        throw new Exception("图片上传失败");
    }
    
    private static byte[] ReadStreamToByteArray(Stream inputStream)
    {
        using MemoryStream memoryStream = new();
        inputStream.CopyTo(memoryStream);
        
        return memoryStream.ToArray();
        
    }
}