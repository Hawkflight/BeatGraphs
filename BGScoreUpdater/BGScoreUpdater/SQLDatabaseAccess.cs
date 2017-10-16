using System;
using System.Data;
using System.Configuration;
using System.Web;
using System.Data.SqlClient;
using System.Collections;

/// <summary>
/// Summary description for SQLDatabaseAccess
/// </summary>
public class SQLDatabaseAccess
{
    public SQLDatabaseAccess()
    {
    }

    /// <summary>
    /// Private Sql Connection used in subsequent functions throughout the class.
    /// </summary>
    private SqlConnection SqlConn;

    /// <summary>
    /// Open the connection.
    /// </summary>
    public void Open()
    {
        if (SqlConn == null)
        {
            SqlConn = new SqlConnection(@"Data Source=DESKTOP-FUDRUQT\SQLEXPRESS;Integrated Security=false;  Initial Catalog=BeatGraphs;user=jbrowne; password=jbrowne; Connection Timeout=50000000");
            SqlConn.Open();
        }
    }

    /// <summary>
    /// Close the connection.
    /// </summary>
    public void Close()
    {
        if (SqlConn != null) SqlConn.Close();
    }

    /// <summary>
    /// Dispose of the connection.
    /// </summary>
    public void Dispose()
    {
        if (SqlConn != null) SqlConn.Dispose();
        SqlConn = null;
    }

    /// <summary>
    /// Creates a SqlParameter based on the given paramter attributes.
    /// </summary>
    /// <param name="ParamName"></param>
    /// <param name="DbType"></param>
    /// <param name="Size"></param>
    /// <param name="Direction"></param>
    /// <param name="Value"></param>
    /// <returns></returns>
    public SqlParameter CreateParameter(string ParamName, SqlDbType DbType, Int32 Size, ParameterDirection Direction, object Value)
    {
        SqlParameter SqlParam;

        if (Size > 0)
            SqlParam = new SqlParameter(ParamName, DbType, Size);
        else
            SqlParam = new SqlParameter(ParamName, DbType);

        SqlParam.Direction = Direction;
        if (!(Direction == ParameterDirection.Output && Value == null))
            SqlParam.Value = Value;

        return SqlParam;
    }

    /// <summary>
    /// Creates a SqlCommand object based on the Stored Procedure Name and given parmeters.
    /// </summary>
    /// <param name="SqlSPName"></param>
    /// <param name="SqlParams"></param>
    /// <returns></returns>
    public SqlCommand CreateCommand(string SqlSPName, SqlParameter[] SqlParams)
    {
        SqlCommand SqlComm = new SqlCommand();
        SqlComm.CommandText = SqlSPName;
        SqlComm.CommandType = CommandType.StoredProcedure;
        if (SqlConn != null) SqlComm.Connection = SqlConn;

        //Jaime added..
        SqlComm.Parameters.Clear();
        //..that's it
        if (SqlParams != null)
        {
            foreach (SqlParameter SqlParam in SqlParams)
            {
                SqlComm.Parameters.Add(SqlParam);
            }
        }
        //JKB

        //JKB
        return SqlComm;
    }

    /// <summary>
    /// Executes a stored procedure that has no parameters.
    /// </summary>
    /// <param name="SqlSPName"></param>
    /// <returns></returns>
    public void ExecuteSqlSP(string SqlSPName)
    {
        SqlCommand SqlComm = CreateCommand(SqlSPName, null);
        SqlComm.ExecuteNonQuery();
        //this.Close();
        //return (int)SqlComm.Parameters["ReturnValue"].Value;
    }

    /// <summary>
    /// Executes a stored procedure that has parameters.
    /// </summary>
    /// <param name="SqlSPName"></param>
    /// <param name="SqlParams"></param>
    /// <returns></returns>
    public void ExecuteSqlSP(string SqlSPName, SqlParameter[] SqlParams)
    {
        SqlCommand SqlComm = CreateCommand(SqlSPName, SqlParams);
        SqlComm.ExecuteNonQuery();
        //this.Close();
        //return SqlComm.Parameters["ReturnValue"].Value;
    }

    /// <summary>
    /// Executes a stored procedure that has no parameters and returns a DataReader Object.
    /// </summary>
    /// <param name="SqlSPName"></param>
    /// <param name="SqlDR"></param>
    public void ExecuteSqlSP(string SqlSPName, out SqlDataReader SqlDR)
    {
        SqlCommand SqlComm = CreateCommand(SqlSPName, null);
        //SqlDR = SqlComm.ExecuteReader(System.Data.CommandBehavior.CloseConnection);
        SqlDR = SqlComm.ExecuteReader();
    }

    /// <summary>
    /// Executes a stored procedure that has parameters and returns a DataReader Object.
    /// </summary>
    /// <param name="SqlSPName"></param>
    /// <param name="SqlParams"></param>
    /// <param name="SqlDR"></param>
    public void ExecuteSqlSP(string SqlSPName, SqlParameter[] SqlParams, out SqlDataReader SqlDR)
    {
        SqlCommand SqlComm = CreateCommand(SqlSPName, SqlParams);
        //SqlDR = SqlComm.ExecuteReader(System.Data.CommandBehavior.CloseConnection);
        SqlDR = SqlComm.ExecuteReader();
    }

    /// <summary>
    /// Executes a stored procedure that has no parameters and returns a DataTable Object.
    /// </summary>
    /// <param name="SqlSPName"></param>
    /// <param name="SqlDT"></param>
    public void ExecuteSqlSP(string SqlSPName, out DataTable SqlDT)
    {
        SqlDataReader SqlDR;
        SqlCommand SqlComm = CreateCommand(SqlSPName, null);
        //SqlDR = SqlComm.ExecuteReader(System.Data.CommandBehavior.CloseConnection);
        SqlDR = SqlComm.ExecuteReader();
        //SqlDT.Load(SqlDR);
        SqlDT = null;
    }

    /// <summary>
    /// Executes a stored procedure that has parameters and returns a DataTable Object.
    /// </summary>
    /// <param name="SqlSPName"></param>
    /// <param name="SqlParams"></param>
    /// <param name="SqlDT"></param>
    public void ExecuteSqlSP(string SqlSPName, SqlParameter[] SqlParams, out DataTable SqlDT)
    {
        SqlDataReader SqlDR;
        SqlCommand SqlComm = CreateCommand(SqlSPName, SqlParams);
        //SqlDR = SqlComm.ExecuteReader(System.Data.CommandBehavior.CloseConnection);
        SqlDR = SqlComm.ExecuteReader();
        //SqlDT.Load(SqlDR);
        SqlDT = null;
    }

    /// <summary>
    /// Executes a stored procedure that has no parameters and returns a DataSet Object.
    /// </summary>
    /// <param name="SqlSPName"></param>
    /// <param name="SqlParams"></param>
    /// <param name="SqlDS"></param>
    public void ExecuteSqlSP(string SqlSPName, out DataSet SqlDS)
    {
        SqlDataReader SqlDR;
        SqlCommand SqlComm = CreateCommand(SqlSPName, null);
        //SqlDR = SqlComm.ExecuteReader(System.Data.CommandBehavior.CloseConnection);
        SqlDR = SqlComm.ExecuteReader();
        //SqlDS.Load(SqlDR);
        SqlDS = null;
    }

    /// <summary>
    /// Executes a stored procedure that has parameters and returns a DataSet Object.
    /// </summary>
    /// <param name="SqlSPName"></param>
    /// <param name="SqlParams"></param>
    /// <param name="SqlDS"></param>
    public void ExecuteSqlSP(string SqlSPName, SqlParameter[] SqlParams, out DataSet SqlDS)
    {
        SqlDataReader SqlDR;
        SqlCommand SqlComm = CreateCommand(SqlSPName, SqlParams);
        //SqlDR = SqlComm.ExecuteReader(System.Data.CommandBehavior.CloseConnection);
        SqlDR = SqlComm.ExecuteReader();
        //SqlDS.Load(SqlDR);
        SqlDS = null;
    }

    public string sqlGet(SqlDataReader sqlDR, string sFieldName)
    {
        return sqlDR.GetSqlValue(sqlDR.GetOrdinal(sFieldName)).ToString();
    }
}