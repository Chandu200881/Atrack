using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace AtrackListener
{    
    public class TcpAtrackService
    {
        readonly TcpClient _tcpClient;
        public TcpAtrackService(TcpClient client)
        {
            _tcpClient = client;
        }

//Date Format: Binary
//40508B06003900020001463868C076A0570DD8EA570DD8E9570DD8E9073EE3A5017EC651001B650000000000080100000000000007D007D0001100B400BD00
//Date format: ASCII
//@P,8B06,57,2,358683066267296,20160413052810,20160413052809,20160413052809,121.562021,25.085521,27,101,0.0,0.8,1,0,0,0,,2000,2000,,1100B400BD00

        public async Task RunTask()
        {
            using (_tcpClient) 
            using (var stream = _tcpClient.GetStream())
            {
                AtrackMain.LogData(DateTime.Now + " Received connection request from " + _tcpClient.Client.RemoteEndPoint);
                var fullPacket = new List<byte>();
                var bytes = new byte[4096];
                int length;
                string imei = string.Empty;
                string raw_data = string.Empty;

                while ((length = await stream.ReadAsync(bytes, 0, bytes.Length)) != 0)
                {
                    raw_data = String.Join("", bytes.Take(length).Select(x => x.ToString("X2")).ToArray());
                    AtrackMain.LogData(string.Format("{0} - received [{1}]", DateTime.Now, raw_data));                   

                    if (length == 12)
                    {
                        imei = AtrackDecoder.ByteArrayToHexString(bytes.Skip(2).Take(8).ToArray());
                        AtrackMain.LogData(imei);
                        AtrackMain.LogImei(imei);      

                        byte[] response = KeepAliveResponse(bytes);
                        await stream.WriteAsync(response, 0, response.Length);
                        AtrackMain.LogData(string.Format("{0} - responded [{1}]", DateTime.Now, String.Join("", response.Select(x => x.ToString("X2")).ToArray())));
                    }
                    else if (length > 80)
                    {
                        var raw = System.Text.Encoding.ASCII.GetString(bytes);
                        string[] totalData = raw.Split(',');
                        string[] header = totalData.Take(5).ToArray();
                        string[] data = totalData.Skip(5).Take(totalData.Length - 1).ToArray();

                        imei = header[4];//unit id
                        string seqid = header[3]; //seq id

                        AtrackMain.LogData(imei);
                        AtrackMain.LogImei(imei);

                        string joinData = string.Join(",", data);
                        var eachData = joinData.Split(new[] { "\r\n" }, StringSplitOptions.None);

                        eachData.ToList().ForEach(i =>
                            {
                                var fields = i.Split(',');
                                if (fields.Length > 1)
                                {
                                    var decodedData = AtrackDecoder.AsciiDecodeGps(fields, imei);
                                    if (decodedData.IMEI != "")
                                    {
                                        AtrackDataAccess.DbUpdate(decodedData,imei,i);
                                    }
                                }
                            });
                        byte[] response = AsciiResponse(imei,seqid);
                        await stream.WriteAsync(response, 0, response.Length);
                        AtrackMain.LogData(string.Format("{0} - responded [{1}]", DateTime.Now, String.Join("", response.Select(x => x.ToString("X2")).ToArray())));
                    }          
                   
                    else if (length < 80)
                    {
                        imei = AtrackDecoder.ByteArrayToHexString(bytes.Skip(8).Take(8).ToArray());
                        AtrackMain.LogData(imei);
                        AtrackMain.LogImei(imei);

                        var decodedData = AtrackDecoder.DecodeGps(bytes);
                        AtrackDataAccess.DbUpdate(decodedData, imei, raw_data);


                        byte[] response = ServerResponse(bytes);
                        await stream.WriteAsync(response, 0, response.Length);
                        AtrackMain.LogData(string.Format("{0} - responded [{1}]", DateTime.Now, String.Join("", response.Select(x => x.ToString("X2")).ToArray())));
                    }                    
                    Array.Clear(bytes, 0, bytes.Length);                    
                }
            }
        }

        private byte[] KeepAliveResponse(byte[] bts)
        {
            byte[] byts = new byte[12];
            byts[0] = 254;
            byts[1] = 2;
            bts.Skip(2).Take(8).ToArray().CopyTo(byts, 2);//unit ID
            bts.Skip(10).Take(2).ToArray().CopyTo(byts, 10);// Seq ID
            return byts;
        }

        private byte[] AsciiResponse(string unit, string seq)
        {
            byte[] byts = new byte[12];
            byts[0] = 254;
            byts[1] = 2;
            AtrackDecoder.StringToBytes(Convert.ToInt64(unit.PadLeft(16,'0')).ToString("X").PadLeft(16, '0')).CopyTo(byts, 2);//unit ID
            AtrackDecoder.StringToBytes(Convert.ToInt64(seq.PadLeft(4, '0')).ToString("X").PadLeft(4, '0')).CopyTo(byts, 10);// Seq ID
            return byts;
        }

        private byte[] ServerResponse(byte[] bts)
        {
            byte[] byts = new byte[12];
            byts[0] = 254;
            byts[1] = 2;
            bts.Skip(8).Take(8).ToArray().CopyTo(byts, 2);//unit ID
            bts.Skip(6).Take(2).ToArray().CopyTo(byts, 10);// Seq ID
            return byts;
        }
    }
}
