using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace YounBot.Utils;

public static class ImageUtils
{
    
    public static async Task<byte[]> UrlToImageMessageAsync(string url)
    {
        using var httpClient = new HttpClient();
        using var response = await httpClient.GetAsync(url);
        
        if (response.IsSuccessStatusCode) 
        {
            var image = ReadStreamToByteArray(await response.Content.ReadAsStreamAsync());
            return image;
        }
        
        throw new Exception("图片上传失败");
    }
    
    private static byte[] ReadStreamToByteArray(Stream inputStream)
    {
        using var memoryStream = new MemoryStream();
        inputStream.CopyTo(memoryStream);
        
        return memoryStream.ToArray();
        
    }
}