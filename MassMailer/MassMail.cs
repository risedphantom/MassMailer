using System.Data;
using System.Data.SqlClient;
using MailService.Singleton;

namespace MailService
{
    public class Mailing
    {
        public string Template { get; set; }
        public string Subject { get; set; }
        public string StateId { get; set; }
        public string MailId { get; set; }
        public string Name { get; set; }
        public string From { get; set; }
    }

    public class MailResult
    {
        public long Id { get; set; }
        public int Result { get; set; }
    }

    class MassMail
    {
        #region --- Constants ---

        public const string DefaultUserAgent = "mass mailer";
        public const string DefaultMode = "";
        public const int DefaultBlockSize = 100;

        #endregion

        #region --- Properties ---

        public static string ConnectionString { get; private set; }
        public string UserAgent { get; private set; }
        public string Mode { get; private set; }
        public static int BlockSize { get; private set; }

        #endregion

        #region --- Constructors ---

        /// <summary>
        /// Creates an instance of object.
        /// (This class throws exceptions when error ocures!)
        /// </summary>
        public MassMail(string connectionString)
            : this(DefaultBlockSize, DefaultUserAgent, connectionString, DefaultMode)
        {
        }

        /// <summary>
        /// Creates an instance of object.
        /// (This class throws exceptions when error ocures!)
        /// </summary>
        public MassMail(string userAgent, string connectionString)
            : this(DefaultBlockSize, userAgent, connectionString, DefaultMode)
        {
        }

        /// <summary>
        /// Creates an instance of object.
        /// (This class throws exceptions when error ocures!)
        /// </summary>
        public MassMail(int batchSize, string userAgent, string connectionString, string mode)
        {
            BlockSize = batchSize;
            UserAgent = userAgent;
            Mode = mode;
            ConnectionString = connectionString;
        }

        #endregion

        #region --- Methods ---

        public DataTable GetBatch()
        {
            using (var conn = new SqlConnection(ConnectionString))
            using (var cmd = new SqlDataAdapter("GetBatchFromQueue", conn))
            using (var ds = new DataSet())
            {
                cmd.SelectCommand.CommandTimeout = 300;
                cmd.SelectCommand.CommandType = CommandType.StoredProcedure;
                cmd.SelectCommand.Parameters.Add(new SqlParameter("@BlockSize", BlockSize));
                cmd.SelectCommand.Parameters.Add(new SqlParameter("@Host", UserAgent));
                cmd.SelectCommand.Parameters.Add(new SqlParameter("@Mode", Mode));
                cmd.Fill(ds);
                return ds.Tables[0].Rows.Count == 0 ? null : ds.Tables[0].Copy();
            }
        }

        public Template GetTemplate(long id)
        {
            using (var conn = new SqlConnection(ConnectionString))
            using (var cmd = new SqlDataAdapter("TemplateGet", conn))
            using (var ds = new DataSet())
            {
                cmd.SelectCommand.CommandTimeout = 300;
                cmd.SelectCommand.CommandType = CommandType.StoredProcedure;
                cmd.SelectCommand.Parameters.Add(new SqlParameter("@ID", id));
                cmd.Fill(ds);

                if (ds.Tables[0].Rows.Count == 0)
                    return null;

                return new Template
                {
                    Id = (long)ds.Tables[0].Rows[0]["ID"],
                    Body = ds.Tables[0].Rows[0]["Body"].ToString(),
                    IsHtml = (bool)ds.Tables[0].Rows[0]["IsHTML"],
                    Guid = ds.Tables[0].Rows[0]["GUID"].ToString()
                };
            }
        }

        public Attach GetAttachment(long id)
        {
            using (var conn = new SqlConnection(ConnectionString))
            using (var cmd = new SqlDataAdapter("AttachmentGet", conn))
            using (var ds = new DataSet())
            {
                cmd.SelectCommand.CommandTimeout = 300;
                cmd.SelectCommand.CommandType = CommandType.StoredProcedure;
                cmd.SelectCommand.Parameters.Add(new SqlParameter("@AttachmentID", id));
                cmd.Fill(ds);

                if (ds.Tables[0].Rows.Count == 0)
                    return null;

                return new Attach
                {
                    Id = id,
                    Name = ds.Tables[0].Rows[0]["Name"].ToString(),
                    Data = (byte[])ds.Tables[0].Rows[0]["Data"]
                };
            }
        }

        public DataTable GetAttachments(long messageId)
        {
            using (var conn = new SqlConnection(ConnectionString))
            using (var cmd = new SqlDataAdapter("GetAttachments", conn))
            using (var ds = new DataSet())
            {
                cmd.SelectCommand.CommandTimeout = 300;
                cmd.SelectCommand.CommandType = CommandType.StoredProcedure;
                cmd.SelectCommand.Parameters.Add(new SqlParameter("@MessageID", messageId));
                cmd.Fill(ds);

                return ds.Tables[0].Rows.Count == 0 ? null : ds.Tables[0].Copy();
            }
        }

        public void UpdateBlock(DataTable block)
        {
            block.AcceptChanges();
            foreach (DataRow row in block.Rows)
                row.SetModified();

            using (var conn = new SqlConnection(ConnectionString))
            using (var cmd = new SqlDataAdapter())
            {
                cmd.UpdateCommand = new SqlCommand(
                    "update ActiveQueue set Status=@Status, SendMoment=@SendMoment "
                    + "where ID=@ID", conn);
                cmd.UpdateCommand.Parameters.Add("@Status", SqlDbType.Int, 4, "Status");
                cmd.UpdateCommand.Parameters.Add("@ID", SqlDbType.BigInt, 8, "ID");
                cmd.UpdateCommand.Parameters.Add("@SendMoment", SqlDbType.DateTime, 8, "SendMoment");
                cmd.UpdateCommand.UpdatedRowSource = UpdateRowSource.None;
                cmd.UpdateBatchSize = block.Rows.Count;
                cmd.Update(block);
            }
        }

        #endregion
    }
}
