using Aras.IOM;
using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArasScheduleMethodBuildApp.Code
{
    class Wrapper_localhost_PLM_JPC_WF_SOPAR_END_Activate
    {
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="connection">HttpServerConnection</param>
        /// <returns>innMethod</returns>
        public Case_JPC_WF_SOPAR_END_Activate init(HttpServerConnection connection)
        {
            Case_JPC_WF_SOPAR_END_Activate innMethod = new Case_JPC_WF_SOPAR_END_Activate(connection);
            return innMethod;
        }
        public class Case_JPC_WF_SOPAR_END_Activate : Item
        {
            public Aras.Server.Core.CallContext CCO { get; set; }
            public Aras.Server.Core.IContextState RequestState { get; set; }
            public Case_JPC_WF_SOPAR_END_Activate(IServerConnection arg) : base(arg)
            {
            }

            /// <summary>
            /// method內容請由這邊填寫
            /// </summary>
            /// <returns>Item</returns>
            ///            //MODE
            public Item MethodCode0()
            {
                #region MethodCode

                // 初始化 Innovator 物件
                inn = this.getInnovator();

                // 主要邏輯封裝於 main 函式
                Item res = main(this);
                return res;
            }
            Innovator inn;
            // main 函式
            Item main(Item thisItem)
            {
                string activityId = thisItem.getProperty("id", "");

                try
                {
                    // 1. 取得工作流程資訊
                    Item wflItem = GetWF(activityId);

                    // 2. 取得表單資訊 (SOPAR物件)
                    Item soparItem = GetFormByWF(wflItem);

                    if (soparItem.isError())
                    {
                        Log("無法取得SOPAR物件: " + soparItem.getErrorString());
                        return inn.newError("無法取得SOPAR物件: " + soparItem.getErrorString());
                    }

                    string soparId = soparItem.getProperty("id", "");
                    //Log("已取得SOPAR物件ID: " + soparId);

                    // 3. 查詢SOPAR關聯的所有SOPAR_Trace物件
                    Item soparTraces = GetSOPARTraces(soparId);

                    if (soparTraces.isError())
                    {
                        Log("無法取得SOPAR_Trace物件: " + soparTraces.getErrorString());
                        return inn.newError("無法取得SOPAR_Trace物件: " + soparTraces.getErrorString());
                    }

                    // 建立用於儲存唯一identity的HashSet
                    HashSet<string> uniqueIdentities = new HashSet<string>();

                    // 4. 循環處理每一個SOPAR_Trace物件
                    for (int i = 0; i < soparTraces.getItemCount(); i++)
                    {
                        Item traceItem = soparTraces.getItemByIndex(i);
                        string ownedById = traceItem.getProperty("owned_by_id", "");

                        if (!string.IsNullOrEmpty(ownedById))
                        {
                            uniqueIdentities.Add(ownedById);
                            Log("找到擁有者ID: " + ownedById);
                        }
                    }

                    // 5. 將所有收集到的identity加入到Activity Assignment
                    foreach (string identityId in uniqueIdentities)
                    {
                        Item result = AddAssignment(activityId, identityId, "0", "0", "1");
                        if (result != null)
                        {
                            Log("成功新增指派: " + identityId);
                        }
                    }

                    return inn.newResult("成功處理SOPAR追蹤指派");
                }
                catch (Exception ex)
                {
                    Log("處理過程發生錯誤: " + ex.Message);
                    return inn.newError("處理過程發生錯誤: " + ex.Message);
                }
            }

            // 取得工作流程資訊
            Item GetWF(string actId)
            {
                // 取出目前節點的workflow物件
                Item wflItem = this.newItem("Workflow", "get");
                wflItem.setAttribute("select", "source_id,source_type,related_id");
                Item wflProc = wflItem.createRelatedItem("Workflow Process", "get");
                wflProc.setAttribute("select", "name,copied_from_string");
                Item wflProcAct = wflProc.createRelationship("Workflow Process Activity", "get");
                wflProcAct.setAttribute("select", "related_id");
                wflProcAct.setProperty("related_id", actId);
                wflItem = wflItem.apply();
                return wflItem;
            }

            // 取得表單資訊
            Item GetFormByWF(Item wflItem)
            {
                string form_id = wflItem.getProperty("source_id");
                string source_type = wflItem.getPropertyAttribute("source_type", "name", "");

                // 確認source_type是否為SOPAR
                if (source_type != "SOPAR")
                {
                    Log("警告: 表單類型不是SOPAR, 而是 " + source_type);
                }

                Item formItm = inn.newItem(source_type, "get");
                formItm.setProperty("id", form_id);
                formItm = formItm.apply();
                return formItm;
            }

            // 取得SOPAR關聯的所有SOPAR_Trace物件
            Item GetSOPARTraces(string soparId)
            {
                // 使用AML查詢獲取相關的SOPAR_Trace物件
                string aml = $@"<AML>
                    <Item type='SOPAR_Trace' action='get'>
                    <source_id>{soparId}</source_id>
                    <owned_by_id condition='is not null'></owned_by_id>
                    </Item>
                </AML>";

                Item traces = inn.applyAML(aml);
                return traces;
            }

            // 新增指派
            Item AddAssignment(string act_id, string identity_id, string is_required, string for_all_members, string voting_weight)
            {
                if (!CheckAddedAssignment(act_id, identity_id))
                {
                    Item newActAssignment = inn.newItem("Activity Assignment", "add");
                    newActAssignment.setProperty("source_id", act_id);
                    newActAssignment.setProperty("related_id", identity_id);
                    newActAssignment.setProperty("is_required", is_required);
                    newActAssignment.setProperty("for_all_members", for_all_members);
                    newActAssignment.setProperty("voting_weight", voting_weight);
                    newActAssignment = newActAssignment.apply();
                    return newActAssignment;
                }
                return null;
            }

            // 檢查指派是否已存在
            bool CheckAddedAssignment(string actId, string identId)
            {
                Item newActAssignment = inn.newItem("Activity Assignment", "get");
                newActAssignment.setProperty("source_id", actId);
                newActAssignment.setProperty("related_id", identId);
                newActAssignment = newActAssignment.apply();
                return !newActAssignment.isError();
            }

            // 記錄日誌
            void Log(string msg)
            {
                Item log = inn.newItem("JPC_Method_Log", "add");
                log.setProperty("jpc_run_method", "JPC_WF_SOPAR_END_Activate");
                log.setProperty("jpc_method_event", "workflow");
                log.setProperty("jpc_log", msg);
                log = log.apply();
            }
            public void r(){
                #endregion MethodCode
            }
        }
    }
}
