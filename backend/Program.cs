using System.Net;
using System.Text;
using System.Text.Json;
using System;

using var listener = new HttpListener();
listener.Prefixes.Add("http://localhost:5000/");
listener.Start();
Console.WriteLine("Server läuft auf Port 5000...");

string getDeviceID(string requestRawUrl){
    String [] split = requestRawUrl.Split("/");
    return split[2];
}

async Task<string> handleRequest(HttpListenerRequest request){
    DeviceManager manager = new();

    switch (request.HttpMethod, request.RawUrl){
        case ("GET", var p) when p.StartsWith("/api/devices/"):
            return manager.getDevice(Int32.Parse(getDeviceID(p))).toJSON();

        case("GET", "/api/devices"):
            return manager.printAll();

        case ("PUT", var p) when p.StartsWith("/api/devices/"):
            if (request.HasEntityBody){
                using StreamReader reader = new StreamReader(request.InputStream, request.ContentEncoding);
                string json = await reader.ReadToEndAsync();

                JsonSerializerOptions opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                UpdateDeviceDto? dto ;
                try{
                    dto = JsonSerializer.Deserialize<UpdateDeviceDto>(json, opts);
                }
                catch (JsonException){
                    return "400";
                }

                Device device = manager.getDevice(Int32.Parse(getDeviceID(p)));
                if (dto != null) device.Update(dto);
            }
            return "200";
        default:
            return "404";
    }
}

while (true)
{
    HttpListenerContext context = listener.GetContext();
    HttpListenerRequest request = context.Request;
    HttpListenerResponse response = context.Response;

    string responseString = handleRequest(request);

    byte[] buffer = Encoding.UTF8.GetBytes(responseString);

    response.ContentType = "text/html; charset=utf-8";
    response.ContentLength64 = buffer.Length;
    using Stream output = response.OutputStream;
    output.Write(buffer, 0, buffer.Length);
}

class Device{
    public int ID { get; set;}
    public string name { get; set;} = "";
    public bool isOn { get; set;}
    virtual public string toJSON(){
        return JsonSerializer.Serialize(this);
    }
    virtual public void Update(UpdateDeviceDto dto){
    }

}
class UpdateDeviceDto {
    public bool IsOn { get; set; }
    abstract public bool IsValid();
}

class DeviceManager{
    public Dictionary<int, Device> allDevices = new(){
        {0, new() {ID = 0, name = "pump", isOn = true}},
        {1, new() {ID = 1, name = "light", isOn = true}},
        {2, new() {ID = 2, name = "heating", isOn = true}}
    };
    public Device getDevice(int id){
        return allDevices[id];
    }
    public string printAll(){
        string result = "";
        foreach (var device in allDevices){
            result += device.Value.toJSON();
            result += ", ";
        }
        return result;
    }
}


