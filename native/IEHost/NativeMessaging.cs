using System.Text;

namespace IEHost;

static class NativeMessaging
{
    public static string? ReadMessage()
    {
        var stdin = Console.OpenStandardInput();
        var lenBytes = new byte[4];
        int offset = 0;

        while (offset < 4)
        {
            int read = stdin.Read(lenBytes, offset, 4 - offset);
            if (read == 0) return null;
            offset += read;
        }

        uint len = BitConverter.ToUInt32(lenBytes, 0);
        if (len == 0 || len > 1024 * 1024) return null;

        var data = new byte[len];
        offset = 0;
        while (offset < (int)len)
        {
            int read = stdin.Read(data, offset, (int)len - offset);
            if (read == 0) return null;
            offset += read;
        }

        return Encoding.UTF8.GetString(data);
    }

    public static void WriteMessage(string json)
    {
        var data = Encoding.UTF8.GetBytes(json);
        var stdout = Console.OpenStandardOutput();
        var lenBytes = BitConverter.GetBytes((uint)data.Length);
        stdout.Write(lenBytes, 0, 4);
        stdout.Write(data, 0, data.Length);
        stdout.Flush();
    }
}
