using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtrackListener
{
    public class AtrackDataAccess
    {
        private static string GetConnectionString()
        {
            return System.Configuration.ConfigurationManager.AppSettings["ConnStringName"];
            //sets the connection string from your web config file "ConnString" is the name of your Connection String

        }

        public static void DbInsert(GpsInfo data)
        {
            SqlConnection conn = new SqlConnection(GetConnectionString());
            string sql = "INSERT INTO [dbo].[TBL_ATRACK_MASTER]" +
                         " (IMEI,Length,Crc,SeqId,GPS_DateTime,Received_DateTime,Longitude,Latitude,Odometer,Heading,Speed) " +
                          "VALUES " +
                         "(@IMEI,@Length,@Crc,@SeqId,@GPS_DateTime,@Received_DateTime,@Longitude,@Latitude,@Odometer,@Heading,@Speed) SELECT SCOPE_IDENTITY()";
            try
            {
                if (conn.State != ConnectionState.Open)
                {
                    conn.Open();
                }
                SqlCommand cmd = new SqlCommand(sql, conn);

                List<SqlParameter> sp = new List<SqlParameter>();
                sp.Add(new SqlParameter() { ParameterName = "@IMEI", SqlDbType = SqlDbType.NVarChar, Value = data.IMEI });
                sp.Add(new SqlParameter() { ParameterName = "@Length", SqlDbType = SqlDbType.Int, Value = data.Length });
                sp.Add(new SqlParameter() { ParameterName = "@Crc", SqlDbType = SqlDbType.NVarChar, Value = data.Crc });
                sp.Add(new SqlParameter() { ParameterName = "@SeqId", SqlDbType = SqlDbType.Int, Value = data.SeqId });
                sp.Add(new SqlParameter() { ParameterName = "@GPS_DateTime", SqlDbType = SqlDbType.DateTime, Value = data.GPS_DateTime });
                sp.Add(new SqlParameter() { ParameterName = "@Received_DateTime", SqlDbType = SqlDbType.DateTime, Value = data.Received_DateTime });
                sp.Add(new SqlParameter() { ParameterName = "@Longitude", SqlDbType = SqlDbType.Float, Value = data.Longitude });
                sp.Add(new SqlParameter() { ParameterName = "@Latitude", SqlDbType = SqlDbType.Float, Value = data.Latitude });
                sp.Add(new SqlParameter() { ParameterName = "@Odometer", SqlDbType = SqlDbType.Float, Value = data.Odometer });
                sp.Add(new SqlParameter() { ParameterName = "@Heading", SqlDbType = SqlDbType.Int, Value = data.Heading });
                sp.Add(new SqlParameter() { ParameterName = "@Speed", SqlDbType = SqlDbType.Int, Value = data.Speed });

                cmd.Parameters.AddRange(sp.ToArray());
                cmd.CommandType = CommandType.Text;
                var id = cmd.ExecuteScalar();

            }
            catch (System.Data.SqlClient.SqlException ex)
            {
                string msg = "Insert Error:";
                msg += ex.Message;
                AtrackMain.LogData(msg);
                throw new Exception(msg);

            }
            finally
            {
                conn.Close();
            }
        }

        public static void RawData(string raw_data, string IMEI, DateTime gps_date)
        {
            SqlConnection conn = new SqlConnection(GetConnectionString());
            string sql = "INSERT INTO TBL_RAW_GPS (IMEI,RawData,GPS_Date) VALUES" +
           "(@IMEI,@RawData,@GPS_Date) SELECT SCOPE_IDENTITY()";
            try
            {
                if (conn.State != ConnectionState.Open)
                {
                    conn.Open();
                }
                SqlCommand cmd = new SqlCommand(sql, conn);

                List<SqlParameter> sp = new List<SqlParameter>()
                {
                    new SqlParameter() {ParameterName = "@IMEI", SqlDbType = SqlDbType.NVarChar, Value= IMEI},
                    new SqlParameter() {ParameterName = "@RawData", SqlDbType = SqlDbType.NVarChar, Value = raw_data},
                    new SqlParameter() {ParameterName = "@GPS_Date", SqlDbType = SqlDbType.DateTime, Value = gps_date},
                };

                cmd.Parameters.AddRange(sp.ToArray());
                cmd.CommandType = CommandType.Text;
                var id = cmd.ExecuteScalar();
            }
            catch (Exception ex)
            {
                AtrackMain.LogData(ex.Message);
            }
            finally
            {
                conn.Close();
            }
        }

        public static void DbUpdate(GpsInfo data, string imei, string rawData)
        {

            string OwnerName = string.Empty;
            string Email = string.Empty;
            string RegNo = string.Empty;
            string mailSent = string.Empty;
            using (SqlConnection conn = new SqlConnection(GetConnectionString()))
            {
                try
                {
                    using (SqlCommand cmd = new SqlCommand("SP_Insert_NewPointware", conn))
                    {
                        List<SqlParameter> sp = new List<SqlParameter>()
                        {
                            new SqlParameter() {ParameterName = "@UNIT_ID", SqlDbType = SqlDbType.NVarChar, Value="AT"+ imei.Substring(11,4)},
                   
                        };
                        sp.Add(new SqlParameter() { ParameterName = "@LON", SqlDbType = SqlDbType.Float, Value = data.Longitude });
                        sp.Add(new SqlParameter() { ParameterName = "@LAT", SqlDbType = SqlDbType.Float, Value = data.Latitude });
                        sp.Add(new SqlParameter() { ParameterName = "@SPEED", SqlDbType = SqlDbType.Int, Value = data.Speed });
                        sp.Add(new SqlParameter() { ParameterName = "@DIRECTION", SqlDbType = SqlDbType.Int, Value = data.Heading });
                        sp.Add(new SqlParameter() { ParameterName = "@LOCAL_DATE_TIME", SqlDbType = SqlDbType.DateTime, Value = data.GPS_DateTime });
                        sp.Add(new SqlParameter() { ParameterName = "@DISTANCE", SqlDbType = SqlDbType.Float, Value = 0 });
                        sp.Add(new SqlParameter() { ParameterName = "@SERVER_TIME", SqlDbType = SqlDbType.VarChar, Value = DateTime.UtcNow });

                        sp.Add(new SqlParameter() { ParameterName = "@ATTRIBUTE1", SqlDbType = SqlDbType.VarChar, Value = data.EventType });//SOS
   
                        sp.Add(new SqlParameter() { ParameterName = "@ATTRIBUTE2", SqlDbType = SqlDbType.VarChar, Value = data.DriverID });//immobilizer

                        sp.Add(new SqlParameter() { ParameterName = "@CHARGING", SqlDbType = SqlDbType.Bit, Value = true });//Charging
                        sp.Add(new SqlParameter() { ParameterName = "@LOWBATT", SqlDbType = SqlDbType.Bit, Value = false });//LOW BATTERY

                        sp.Add(new SqlParameter() { ParameterName = "@IGNITION", SqlDbType = SqlDbType.Bit, Value = data.EventType == "IgnitionOn"? true: false });//IGNITION
                        sp.Add(new SqlParameter() { ParameterName = "@ANALOG_DATA", SqlDbType = SqlDbType.NVarChar, Value = "" });//ANALOG_DATA
                        sp.Add(new SqlParameter() { ParameterName = "@status", SqlDbType = SqlDbType.NVarChar, Value = data.EventType == "IdleOn" ? "ON" : "OFF" });

                        sp.Add(new SqlParameter() { ParameterName = "@reportID", SqlDbType = SqlDbType.Int, Value = data.ReportID});
                        sp.Add(new SqlParameter() { ParameterName = "@imei", SqlDbType = SqlDbType.NVarChar, Value = data.IMEI});
                        sp.Add(new SqlParameter() { ParameterName = "@eventValue", SqlDbType = SqlDbType.NVarChar, Value = data.EventType == "RFID" ? data.DriverID : data.EventType });
                        sp.Add(new SqlParameter() { ParameterName = "@validEvent", SqlDbType = SqlDbType.Bit, Value = data.ValidEvent});

                        sp.Add(new SqlParameter() { ParameterName = "@RAW_DATA", SqlDbType = SqlDbType.NVarChar, Value = rawData });
                        sp.Add(new SqlParameter() { ParameterName = "@ServerDateTime", SqlDbType = SqlDbType.DateTime, Value = DateTime.UtcNow });

                        cmd.Parameters.AddRange(sp.ToArray());
                        cmd.CommandType = CommandType.StoredProcedure;
                        conn.Open();
                        cmd.ExecuteNonQuery();                        
                    }
                }
                catch (System.Data.SqlClient.SqlException ex)
                {
                    string msg = "Insert Error:";
                    msg += ex.Message;
                    throw new Exception(msg);

                }
                catch (Exception ex)
                {
                    string msg = "Insert Error";
                }
                finally
                {
                    conn.Close();
                }
            }

        }
    }
}
