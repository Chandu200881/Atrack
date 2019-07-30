using System;

namespace AtrackListener
{
    public struct GpsInfo
    {
        private string _event;

        public string IMEI { get; private set; }
        public int SeqId { get; private set; }
        public int Length { get; private set; }
        public string Crc { get;private set; }
        public DateTime GPS_DateTime { get;private set; }
        public DateTime Received_DateTime { get; private set; }
        public double Longitude { get; private set; }
        public double Latitude { get; private set; }
        public int Heading { get; private set; }
        public double Odometer { get; private set; }
        public int Satellites { get; private set; }
        public int Speed { get; private set; }
        public int ReportID { get; private set; }
        public string DriverID { get; private set; }
        public string EventType { get { return _event; } private set { _event = getEventType(value); } }
        public bool ValidEvent { get; private set; }
        //public bool Ignition { get; private set; }
        //public bool Idle { get; private set; }
        

        public static GpsInfo Create(string imei, int seqid, int length, string crc, DateTime gpsDateTime, DateTime recDateTime, double lon, double lat, 
            int heading, double odometer, int satellites, int speed, int reportId, string driverID, string eventType, bool validEvent)
        {
            return new GpsInfo
            {
                IMEI = imei,
                SeqId = seqid,
                Length = length,
                Crc = crc,
                GPS_DateTime = gpsDateTime,
                Received_DateTime = recDateTime,
                Longitude = lon,
                Latitude = lat,
                Odometer =  odometer,
                Heading = heading,
                Satellites = satellites,
                Speed = speed,
                ReportID = reportId,
                DriverID = driverID,
                EventType = eventType,
                ValidEvent = validEvent
                //Ignition = ignition,
                //Idle = idle
            };
        }

        private string getEventType(string val)
        {
            string info = string.Empty;
            switch (val)
            {
                case "2": { info = "Tracking"; } break;
                case "10": { info = "RFID"; } break;
                case "111": { info = "IgnitionOn"; } break;
                case "112": { info = "IgnitionOff"; } break;
                case "113": { info = "IdleOn"; } break;
                case "114": { info = "IdleOff"; } break;
                default: { info = val; } break;
            }
            return info;
        }
    }
}
