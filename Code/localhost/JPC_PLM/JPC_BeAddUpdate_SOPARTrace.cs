using Aras.IOM;
using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArasMethodVScode.Code
{
    class Wrapper_localhost_PLM_JPC_BeAddUpdate_SOPARTrace
    {
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="connection">HttpServerConnection</param>
        /// <returns>innMethod</returns>
        public Case_JPC_BeAddUpdate_SOPARTrace init(HttpServerConnection connection)
        {
            Case_JPC_BeAddUpdate_SOPARTrace innMethod = new Case_JPC_BeAddUpdate_SOPARTrace(connection);
            return innMethod;
        }
        public class Case_JPC_BeAddUpdate_SOPARTrace : Item
        {
            public Aras.Server.Core.CallContext CCO { get; set; }
            public Aras.Server.Core.IContextState RequestState { get; set; }
            public Case_JPC_BeAddUpdate_SOPARTrace(IServerConnection arg) : base(arg)
            {
            }

            /// <summary>
            /// method內容請由這邊填寫
            /// </summary>
            /// <returns>Item</returns>
            /// 
            
            #region AI 可修改的位置
            //MODE
            public Item MethodCode0()
            {
                #region MethodCode
                inn = this.getInnovator();

                string jpc_rfq = this.getProperty("jpc_rfq", "");
                string jpc_part = this.getProperty("jpc_part", "");
                
                // 如果沒有料號，則自動從 RFQ 資料帶入
                if (string.IsNullOrEmpty(jpc_part))
                {
                    PopulatePartAndDrawingInfo(jpc_rfq);
                }

                return this;
            }

            /// <summary>
            /// 根據 RFQ ID 填入零件和工程圖資訊
            /// </summary>
            /// <param name="rfqId">RFQ ID</param>
            private void PopulatePartAndDrawingInfo(string rfqId)
            {
                if (string.IsNullOrEmpty(rfqId)) return;

                Item rfqItem = GetRFQ(rfqId);
                if (rfqItem.isError()) return;

                string rfqPart = rfqItem.getProperty("jpc_part", "");
                if (string.IsNullOrEmpty(rfqPart)) return;

                // 設定零件 ID
                this.setProperty("jpc_part", rfqPart);
                
                // 尋找關聯工程圖
                FindAndSetEngineeringDrawing(rfqPart);
            }

            /// <summary>
            /// 尋找並設定零件的工程圖資訊
            /// </summary>
            /// <param name="partId">零件 ID</param>
            private void FindAndSetEngineeringDrawing(string partId)
            {
                Item partItem = inn.getItemById("Part", partId);
                if (partItem.isError()) return;

                string partNumber = partItem.getProperty("item_number", "");
                if (string.IsNullOrEmpty(partNumber)) return;

                const string cadClassification = "機構/D_D-Engineer Drawing(工程圖)";
                Item partCadItem = GetPartCAD(partNumber, cadClassification);
                
                if (!partCadItem.isError() && partCadItem.getItemCount() > 0)
                {
                    string firstCadId = partCadItem.getItemByIndex(0).getProperty("related_id", "");
                    if (!string.IsNullOrEmpty(firstCadId))
                    {
                        this.setProperty("jpc_drwe", firstCadId);
                    }
                }
            }

            /// <summary>
            /// 根據 ID 取得 RFQ 資料
            // <summary>
            /// <param name="id">RFQ ID</param>
            /// <returns>RFQ Item 物件</returns>
            public Item GetRFQ(string id)
            {
                Item rfqItem = inn.newItem("JPC RFQ", "get");
                rfqItem.setProperty("id", id);
                return rfqItem.apply();
            }

            /// <summary>
            /// 根據料號和分類取得零件 CAD 關聯
            /// </summary>
            /// <param name="partNumber">料號</param>
            /// <param name="classification">CAD 分類</param>
            /// <returns>Part CAD 關聯物件</returns>
            public Item GetPartCAD(string partNumber, string classification)
            {
                string aml = @"<AML>
                                <Item type='Part CAD' action='get'>
                                    <source_id>
                                    <Item type='Part' action='get'>
                                        <item_number>{1}</item_number>
                                    </Item>
                                    </source_id>
                                    <related_id>
                                    <Item type='CAD' action='get'>
                                        <classification>{0}</classification>
                                    </Item>
                                    </related_id>
                                </Item>
                               </AML>";
                
                string formattedAml = string.Format(aml, classification, partNumber);
                return inn.applyAML(formattedAml);
            }

            #endregion AI 可修改的位置
            Innovator inn;
            public void r()
            {
                #endregion MethodCode
            }
            
            
        }
    }
}
