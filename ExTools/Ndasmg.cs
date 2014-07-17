using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using Newtonsoft.Json;
using Imgo.Share;
using Imgo.Share.DataEncrypt;

namespace Imgo.MultiGet.ExTools
{
    public class JsonRequestNDASMG
    {
        public string EpochTime;
        public string Sql;
    }

    public class JsonResponseNDASMG
    {
        public int Status;
        public string StatusDesc;
        public DataSet DataSet;
    }

    public class JsonResponseNDASMG2
    {
        public int Status;
        public string StatusDesc;
        public int AffectedRows;
    }

    /// <summary>
    /// NDAS接口类型定义
    /// </summary>
    public class NdasType
    {
        public static readonly string UserConnect = "UserConnect";
        public static readonly string UserDisconnect = "UserDisconnect";
        public static readonly string MediaServerInit = "MediaServerInit";
        public static readonly string MediaServerHeartbeat = "MediaServerHeartbeat";
        public static readonly string MediaServerShutdown = "MediaServerShutdown";
        public static readonly string ScheduleStorageInit = "ScheduleStorageInit";
        public static readonly string GetCDL = "GetCDL";
        public static readonly string ScheduleNodeInit = "ScheduleNodeInit";
        public static readonly string GetLoadbalList = "GetLoadbalList";
        public static readonly string SetFileScheduleStatus = "SetFileScheduleStatus";
        public static readonly string NodeResMonServerInit = "NodeResMonServerInit";
        public static readonly string NodeResMonGetList = "NodeResMonGetList";
        public static readonly string NodeResMonReport = "NodeResMonReport";
        public static readonly string NodeFileCollectServerInit = "NodeFileCollectServerInit";
        public static readonly string NodeFileCollectReport = "NodeFileCollectReport";
        public static readonly string FileInject = "FileInject";
        public static readonly string FileUnject = "FileUnject";
        public static readonly string FileOnline = "FileOnline";
        public static readonly string FileUnline = "FileUnline";
        public static readonly string CDN3PreloadedFeedback = "CDN3PreloadedFeedback";
        public static readonly string GetDeliveryProfileList = "GetDeliveryProfileList";
        public static readonly string SetFileScheduleSuccess = "SetFileScheduleSuccess";
        public static readonly string GetFeedbackTask = "GetFeedbackTask";
        public static readonly string SetFeedbackTaskStatus = "SetFeedbackTaskStatus";
        public static readonly string QuerySql = "QuerySql";
        public static readonly string ExecSql = "ExecSql";
        public static readonly string ExecProc = "ExecProc";
    }

    /// <summary>
    /// NDAS数据代理接口
    /// </summary>
    public class Ndasmg
    {
        /// <summary>
        /// 从数据库获取待检测文件清单
        /// </summary>
        /// <param name="deliveryProfileNos">分发策略号串</param>
        /// <param name="isExpectOwnerNode">是否排除本节点已入库的文件</param>
        /// <param name="ownerNodeId">本节点ID</param>
        /// <param name="expectOtherNodes">要排除的其他节点的ID串</param>
        /// <param name="isExpectCDL">是否排除本节点和排除节点在分发队列中的文件</param>
        /// <returns></returns>
        public static DataSet GetFiles(string deliveryProfileNos, bool isExpectOwnerNode, short ownerNodeId, string expectOtherNodes, bool isExpectCDL)
        {
            string expectNodeIds = "";
            if (isExpectOwnerNode)
            {
                expectNodeIds += string.Format("{0},", ownerNodeId);
            }
            if (!string.IsNullOrEmpty(expectOtherNodes))
            {
                expectNodeIds += string.Format("{0},", expectOtherNodes);
            }
            expectNodeIds = expectNodeIds.Trim(',');
            string url = string.Format("{0}/{1}", Cfgger.NdasUrl, NdasType.QuerySql);
            string sql = "SELECT fileId, fileName FROM vod_file";
            string sqlWhere = "";
            if (!string.IsNullOrEmpty(deliveryProfileNos))
            {
                sqlWhere += string.Format(" AND deliveryProfileNo IN ({0})", deliveryProfileNos);
            }
            if (!string.IsNullOrEmpty(expectNodeIds))
            {
                sqlWhere += string.Format(" AND fileId NOT IN (SELECT fileId FROM vod_file_node WHERE nodeId IN ({0}))", expectNodeIds);
                if (isExpectCDL) //排除在队列中的文件
                {
                    sqlWhere += string.Format(" AND fileId NOT IN (SELECT fileId FROM vod_cdl WHERE dstNodeId IN ({0}))", expectNodeIds);
                }
            }
            else
            {
                if (isExpectCDL)
                {
                    sqlWhere += string.Format(" AND fileId NOT IN (SELECT fileId FROM vod_cdl WHERE dstNodeId IN ({0}))", ownerNodeId);
                }
            }
            if(!string.IsNullOrEmpty(sqlWhere))
            {
                sql += " WHERE " + sqlWhere.TrimStart(" AND ".ToCharArray());
            }
            sql += ";";
            try
            {
                DataSet ds = QuerySql(url, sql);
                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    Logger.Verb("从数据库获取待检测文件清单成功。Url={0}，Sql={1}, FileCount={2}", 
                        url, sql, ds.Tables[0].Rows.Count);
                    return ds;
                }
                else
                {
                    Logger.Warn("从数据库获取待检测文件清单失败。Url={0}，Sql={1}, ErrInfo={2}",
                    url, sql, "无数据");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("从数据库获取待检测文件清单失败。Url={0}，Sql={1}, ErrInfo={2}",
                    url, sql, ex.Message);
                return null;
            }
        }

        /// <summary>
        /// 获取本节点信息
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="nodeHomeDir"></param>
        /// <returns></returns>
        public static bool GetNodeInfo(out short nodeId, out string nodeHomeDir)
        {
            nodeId = 0;
            nodeHomeDir = "";
            string nodeName = "";
#if DEBUG
            nodeName = "CDNTest69";
#else
            try
            {
                nodeName = System.Net.Dns.GetHostName().Replace("'", "''");
            }
            catch (Exception ex)
            {
                Logger.Error("无法获取本节点机器名。ErrInfo={0}", ex.Message);
                return false;
            }
#endif
            string url = string.Format("{0}/{1}", Cfgger.NdasUrl, NdasType.QuerySql);
            string sql = string.Format(@"SELECT nodeId, homeDir FROM vod_node WHERE nodeName='{0}';", nodeName);
            try
            {
                DataSet ds = QuerySql(url, sql);
                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    nodeId = Convert.ToInt16(ds.Tables[0].Rows[0]["nodeId"]);
                    nodeHomeDir = ds.Tables[0].Rows[0]["homeDir"].ToString();
                    Logger.Verb("从数据库获取本节点信息成功。Url={0}，NodeName={1}，NodeId={2}，NodeHomeDir={3}", 
                        url, nodeName, nodeId, nodeHomeDir);
                    return true;
                }
                else
                {
                    Logger.Error("从数据库获取本节点信息失败。Url={0}，NodeName={1}，ErrInfo={2}",
                        url, nodeName, "无记录");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("从数据库获取本节点信息失败。Url={0}，NodeName={1}，ErrInfo={2}",
                    url, nodeName, ex.Message);
                return false;
            }
        }


        /// <summary>
        /// 文件节点数据入库
        /// </summary>
        /// <param name="fileId"></param>
        /// <param name="fileName"></param>
        /// <param name="nodeId"></param>
        /// <param name="isSuccess">文件检查是否有效或恢复后是否有效</param>
        /// <param name="errMsg">文件无效错误描述</param>
        /// <param name="enableNotifyCdn3Preload">是否通知第三方CDN预分发</param>
        /// <param name="enableFeedbackMas">是否向媒资发送反馈</param>
        /// <returns></returns>
        public static bool NodeFileIntoDB(string fileId, string fileName, short nodeId, bool isSuccess, string failedMsg,
            bool enableNotifyCdn3Preload, bool enableFeedbackMas)
        {
            fileId = fileId.Replace("'", "''");
            failedMsg = failedMsg.Replace("'", "''");
            string url = string.Format("{0}/{1}", Cfgger.NdasUrl, NdasType.ExecProc);
            string sql = string.Format("call utils_add_node_file('{0}', {1}, {2}, '{3}', {4}, {5})",
                fileId, nodeId, (isSuccess ? 1 : 0), failedMsg, (enableNotifyCdn3Preload ? 1 : 0), (enableFeedbackMas ? 1 : 0));

            int retried = 0;
            DataSet ds = null;
            do
            {
                try
                {
                    ds = ExecProc(url, sql);
                    break;
                }
                catch (Exception ex)
                {
                    Logger.Error("入库文件节点数据失败。FileId={0}，FileName={1}，Sql={2}，Url={3}，ErrInfo={4}",
                        fileId, fileName, sql, url, ex.Message);
                    retried += 1; 
                    System.Threading.Thread.Sleep(1000);
                }
            }
            while(retried < 100);

            if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {
                int dbRetVal = Convert.ToInt32(ds.Tables[0].Rows[0][0]);
                if (dbRetVal == 1)
                {
                    Logger.Verb("入库文件节点数据成功。FileId={0}，FileName={1}，Sql={2}，Url={3}",
                        fileId, fileName, sql, url);
                    return true;
                }
                else
                {
                    Logger.Error("入库文件节点数据失败。FileId={0}，FileName={1}，Sql={2}，Url={3}，ErrInfo=存储过程返回非期望值{4}",
                        fileId, fileName, sql, url, dbRetVal);
                    return false;
                }
            }
            else
            {
                Logger.Error("入库文件节点数据失败。FileId={0}，FileName={1}，Sql={2}，Url={3}，ErrInfo=存储过程无结果集返回",
                        fileId, fileName, sql, url);
                return false;
            }
        }


        #region 管理接口
        /// <summary>
        /// 执行QuerySql管理接口，返回结果集
        /// </summary>
        /// <param name="url"></param>
        /// <param name="sql"></param>
        /// <returns></returns>
        public static DataSet QuerySql(string url, string sql)
        {
            try
            {
                JsonRequestNDASMG jsonReq = new JsonRequestNDASMG();
                jsonReq.EpochTime = Imgo.Share.DataConvert.EpochTimeHexStr();
                jsonReq.Sql = sql;

                string jsonReqStr = JsonConvert.SerializeObject(jsonReq);
                string jsonResStrEnc = DES3.Encrypt(jsonReqStr, Cfgger.NdasAuthKey, Cfgger.NdasAuthIv);

                string jsonRspStr = "";
                string errInfo = "";
                if (!HttpHelper.SendHttpPost(url,
                    jsonResStrEnc, Cfgger.NdasTimeoutMsec,
                    null, null, false, Encoding.UTF8, null, out jsonRspStr, out errInfo))
                {
                    throw new Exception(errInfo);
                }

                jsonRspStr = DES3.Decrypt(jsonRspStr, Cfgger.NdasAuthKey, Cfgger.NdasAuthIv);
                JsonResponseNDASMG jsonRsp = JsonConvert.DeserializeObject<JsonResponseNDASMG>(jsonRspStr);
                if (jsonRsp.Status == 1)
                {
                    return jsonRsp.DataSet;
                }
                else
                {
                    throw new Exception(jsonRsp.StatusDesc);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        /// <summary>
        /// 执行ExecSql管理接口，并返回受影响的行数
        /// </summary>
        /// <param name="url"></param>
        /// <param name="sql"></param>
        /// <returns></returns>
        public static int ExecSql(string url, string sql)
        {
            try
            {
                JsonRequestNDASMG jsonReq = new JsonRequestNDASMG();
                jsonReq.EpochTime = Imgo.Share.DataConvert.EpochTimeHexStr();
                jsonReq.Sql = sql;

                string jsonReqStr = JsonConvert.SerializeObject(jsonReq);
                string jsonResStrEnc = DES3.Encrypt(jsonReqStr, Cfgger.NdasAuthKey, Cfgger.NdasAuthIv);

                string jsonRspStr = "";
                string errInfo = "";
                if (!HttpHelper.SendHttpPost(url,
                    jsonResStrEnc, Cfgger.NdasTimeoutMsec,
                    null, null, false, Encoding.UTF8, null, out jsonRspStr, out errInfo))
                {
                    throw new Exception(errInfo);
                }

                jsonRspStr = DES3.Decrypt(jsonRspStr, Cfgger.NdasAuthKey, Cfgger.NdasAuthIv);
                JsonResponseNDASMG2 jsonRsp = JsonConvert.DeserializeObject<JsonResponseNDASMG2>(jsonRspStr);
                if (jsonRsp.Status == 1)
                {
                    return jsonRsp.AffectedRows;
                }
                else
                {
                    throw new Exception(jsonRsp.StatusDesc);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        /// <summary>
        /// 执行ExecProc管理接口，并返回结果集
        /// </summary>
        /// <param name="url"></param>
        /// <param name="sql"></param>
        /// <returns></returns>
        public static DataSet ExecProc(string url, string sql)
        {
            try
            {
                JsonRequestNDASMG jsonReq = new JsonRequestNDASMG();
                jsonReq.EpochTime = Imgo.Share.DataConvert.EpochTimeHexStr();
                jsonReq.Sql = sql;

                string jsonReqStr = JsonConvert.SerializeObject(jsonReq);
                string jsonResStrEnc = DES3.Encrypt(jsonReqStr, Cfgger.NdasAuthKey, Cfgger.NdasAuthIv);

                string jsonRspStr = "";
                string errInfo = "";
                if (!HttpHelper.SendHttpPost(url,
                    jsonResStrEnc, Cfgger.NdasTimeoutMsec,
                    null, null, false, Encoding.UTF8, null, out jsonRspStr, out errInfo))
                {
                    throw new Exception(errInfo);
                }

                jsonRspStr = DES3.Decrypt(jsonRspStr, Cfgger.NdasAuthKey, Cfgger.NdasAuthIv);
                JsonResponseNDASMG jsonRsp = JsonConvert.DeserializeObject<JsonResponseNDASMG>(jsonRspStr);
                if (jsonRsp.Status == 1)
                {
                    return jsonRsp.DataSet;
                }
                else
                {
                    throw new Exception(jsonRsp.StatusDesc);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        #endregion
    }
}
