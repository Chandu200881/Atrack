using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtrackListener
{
    public class AtrackDecoder
    {

        private static string ByteArrayToString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }
        private static DateTime ConvertFromUnixTimestamp(double timestamp)
        {
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            return origin.AddSeconds(timestamp);
        }

        private static string Hex2Decimal(string val)
        {
            return long.Parse(val, System.Globalization.NumberStyles.HexNumber).ToString();
        }

        public static string ByteArrayToHexString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2}", b);
            return long.Parse(hex.ToString(), System.Globalization.NumberStyles.HexNumber).ToString();
        }
        public static byte[] StringToBytes(string data)
        {
            var array = new byte[data.Length / 2];

            var substring = 0;
            for (var i = 0; i < array.Length; i++)
            {
                array[i] = Byte.Parse(data.Substring(substring, 2), NumberStyles.AllowHexSpecifier);
                substring += 2;
            }

            return array;
        }
        public static GpsInfo DecodeGps(byte[] data)
        {
            // Header begin//
            string crc = ByteArrayToHexString(data.Skip(2).Take(2).ToArray());
            int length = int.Parse(ByteArrayToHexString(data.Skip(4).Take(2).ToArray()));
            int seqId = int.Parse(ByteArrayToHexString(data.Skip(6).Take(2).ToArray()));
            string imei = ByteArrayToHexString(data.Skip(8).Take(8).ToArray());//.Substring(0, 15);
            // Header End//

            // Data Begin//
            var time = int.Parse(ByteArrayToHexString(data.Skip(16).Take(4).ToArray()));
            DateTime gpsTime = ConvertFromUnixTimestamp(time);
            var rtime = int.Parse(ByteArrayToHexString(data.Skip(24).Take(4).ToArray()));
            DateTime recTime = ConvertFromUnixTimestamp(rtime);

            double lon = double.Parse(ByteArrayToHexString(data.Skip(28).Take(4).ToArray())) * 0.000001;
            double lat = double.Parse(ByteArrayToHexString(data.Skip(32).Take(4).ToArray())) * 0.000001;

            int heading = int.Parse(ByteArrayToHexString(data.Skip(36).Take(2).ToArray()));
            int reportId = int.Parse(ByteArrayToHexString(data.Skip(38).Take(1).ToArray()));
            double odometer = double.Parse(ByteArrayToHexString(data.Skip(39).Take(4).ToArray())) * 0.1;

            int speed = int.Parse(ByteArrayToHexString(data.Skip(46).Take(2).ToArray()));

            // Data End//

            return GpsInfo.Create(imei,seqId,length,crc,gpsTime, recTime,lon,lat,heading,odometer,0,speed,0,"","",true);
        }

        public static GpsInfo AsciiDecodeGps(string[] data, string imei)
        {
            int reportId = int.Parse(data[6]);
            DateTime gpsTime = ConvertFromUnixTimestamp(Convert.ToDouble(data[0])).AddHours(4);
            DateTime recTime = ConvertFromUnixTimestamp(Convert.ToDouble(data[2])).AddHours(4);

            double lon = double.Parse(data[3]) * 0.000001;
            double lat = double.Parse(data[4]) * 0.000001;

            int heading = int.Parse(data[5]);

            double odometer = double.Parse(data[7]) * 0.1;

            int speed = int.Parse(data[10]);
            string driverID = data[13];

            string eventType = reportId.ToString();

            return GpsInfo.Create(imei, 0, 0, "", gpsTime, recTime, lon, lat, heading, odometer, 0, speed, reportId, driverID, eventType,true);
        }
    }
}
