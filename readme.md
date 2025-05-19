# Aras PLM 方法開發工具

本工具為 Aras PLM 方法開發環境，使用 Visual Studio Code 結合 MCP Server 來簡化 Aras PLM 的自訂方法開發流程。

## 功能概述

- 從 Aras PLM 伺服器查詢現有方法
- 產生新的方法程式碼
- 依據模板結構建立標準化方法
- 提供 Aras C# API 參考文件
- MCP專案參考 [panda0909/mssql_mcp_server](https://github.com/panda0909/mssql_mcp_server/tree/main)
![範例](/Images/image1.png)

## 目錄結構

```
ArasMethodVScode/
│
├── ArasCSharpAPICodeBook/      # Aras C# API 參考文件
│   ├── E_*.htm                 # 事件相關參考文件
│   ├── Events_T_*.htm          # 事件類型參考文件
│   ├── F_*.htm                 # 欄位參考文件
│   ├── Fields_T_*.htm          # 欄位類型參考文件
│   └── M_*.htm                 # 方法參考文件
│
├── Code/                       # 方法程式碼
│   ├── IProgram.cs             # 程式介面定義
│   ├── template_program.txt    # 程式碼模板
│   └── ...                     # 其他程式碼
│
└── Images/                     # 相關圖片資源
```

## 使用方式

### 1. 查詢現有方法

透過 MCP Tool 執行以下 SQL 查詢來取得現有方法：

```sql
-- 查詢特定名稱的方法
SELECT name, method_code
FROM innovator.method
WHERE name = '方法名稱' AND is_current = '1'

-- 查詢符合關鍵字的所有方法
SELECT name, method_code
FROM innovator.method
WHERE name LIKE '%關鍵字%' AND is_current = '1'
ORDER BY name
```

### 2. 產生新方法

使用提供的模板框架來產生新方法：

```csharp
using Aras.IOM;
using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArasScheduleMethodBuildApp.Code
{
    class Wrapper_{@Domain}_{@Database}_{@ClassName}
    {
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="connection">HttpServerConnection</param>
        /// <returns>innMethod</returns>
        public Case_{@ClassName} init(HttpServerConnection connection)
        {
            Case_{@ClassName} innMethod = new Case_{@ClassName}(connection);
            return innMethod;
        }
        public class Case_{@ClassName} : Item
        {
            public Aras.Server.Core.CallContext CCO { get; set; }
            public Aras.Server.Core.IContextState RequestState { get; set; }
            public Case_{@ClassName}(IServerConnection arg) : base(arg)
            {
            }

            /// <summary>
            /// method內容請由這邊填寫
            /// </summary>
            /// <returns>Item</returns>
            /// 

            //MODE
            public Item MethodCode0()
            {
                #region MethodCode
                {@code}
                #endregion MethodCode
            }
        }
    }
}
```

### 3. 常用 Server Event 方法模式

#### 工作流程資訊取得

```csharp
private Item GetWF(string actId) {
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
```

#### 表單資訊取得

```csharp
private Item GetFormByWF(Item wflItem) {
    string form_id = wflItem.getProperty("source_id");
    string source_type = wflItem.getPropertyAttribute("source_type", "name", "");
    Item formItm = inn.newItem(source_type, "get");
    formItm.setProperty("id", form_id);
    formItm = formItm.apply();
    return formItm;
}
```

#### 工作流程路徑操作

```csharp
// 讀取後續路徑
public Item GetNextPaths(string actID) {
    Item path = inn.newItem("Workflow Process Path", "get");
    path.setProperty("source_id", actID);
    path = path.apply();
    return path;
}

// 更新路徑之Default Path
public Item UpdatePath(string pathID, string is_default) {
    Item path = inn.newItem("Workflow Process Path", "edit");
    path.setAttribute("where", "id='" + pathID + "'");
    path.setProperty("is_default", is_default);
    path = path.apply();
    return path;
}
```

#### 工作流程指派管理

```csharp
public Item AddAssignment(string act_id, string identity_id, string is_required, string for_all_members, string voting_weight) {
    if(CheckAddedAssignment(act_id, identity_id) == false) {
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
```

## 方法開發指南

### Aras PLM Server Event 方法開發流程

1. **確定方法需求**：
   - 方法名稱
   - 功能描述
   - 觸發條件
   - 輸入參數
   - 處理邏輯
   - 輸出結果

2. **撰寫方法程式碼**：
   - 套用標準模板
   - 實作處理邏輯
   - 處理錯誤及例外
   - 完善註解

3. **測試及部署**：
   - 本地測試
   - 上傳至 Aras PLM 伺服器
   - 設定方法觸發條件
   - 驗證功能

### 最佳實踐

1. **命名慣例**：使用有意義且符合團隊標準的方法名稱
2. **錯誤處理**：正確處理所有可能的錯誤情境
3. **效能考量**：大量資料處理時使用批次方式提高效能
4. **交易處理**：需要保證資料一致性時，使用交易式處理
5. **平行處理**：需要處理多個項目時，考慮使用平行處理
6. **安全性**：敏感操作需進行適當的權限檢查

### SQL 查詢工具

使用 MCP Tool 執行 SQL 查詢來分析或擷取 Aras PLM 系統資料：

```sql
-- 範例：查詢特定類型的所有項目
SELECT id, created_on, created_by_id 
FROM innovator.[ItemType]
WHERE [condition]
```

## API 參考資源

開發 Aras PLM Server Event 方法時，可參考 ArasCSharpAPICodeBook 資料夾中的 .htm 檔案，了解各種 Aras C# API 的用法。

### 常用 API 類別

- **Item 類別**：`Aras.IOM.Item` - 處理 Aras 中的所有項目
- **連線相關**：`Aras.IOM.HttpServerConnection` - 處理與 Aras 伺服器的連線
- **國際化**：`Aras.IOM.I18NSessionContext` - 處理多語言和地區設定
- **檔案管理**：
  - `Aras.IOME.CheckinManager` - 處理檔案簽入
  - `Aras.IOME.CheckoutManager` - 處理檔案簽出

## MCP SQL 查詢工具

MCP (Model Context Protocol) Server 提供了一個強大的 SQL 查詢工具，可以直接從 Visual Studio Code 環境中查詢 Aras PLM 資料庫：

### 使用方式

1. 在 VS Code 中執行查詢：

```csharp
// 執行 SQL 查詢範例
var query = "SELECT TOP 10 name, method_code FROM innovator.method WHERE is_current = '1'";
var results = bb7_execute_sql(query);
```

2. 取得方法程式碼並產生新方法檔案：

```csharp
// 取得指定方法的程式碼
var methodQuery = "SELECT method_code FROM innovator.method WHERE name = 'YourMethodName' AND is_current = '1'";
var methodCode = bb7_execute_sql(methodQuery);

// 使用取得的程式碼填入模板並產生新方法
// ...
```

### 查詢類型範例

```sql
-- 查詢指定項目類型的屬性
SELECT P.name, P.data_type, P.data_source, P.label 
FROM innovator.property P
INNER JOIN innovator.itemtype IT ON P.source_id = IT.id
WHERE IT.name = 'Part' AND P.is_current = '1'

-- 查詢工作流程定義
SELECT * FROM innovator.workflow_map WHERE is_current = '1'

-- 查詢使用者資訊
SELECT * FROM innovator.user WHERE is_current = '1'
```

## 注意事項

1. 遵循 Aras 方法命名慣例
2. 確保程式碼包含適當的註解
3. 避免使用保留字作為變數名稱
4. 所有資料庫操作應包含適當的錯誤處理
5. 大型操作應考慮批次處理以避免效能問題
6. 資安敏感操作需進行適當的權限檢查


## 授權資訊

本工具僅供內部開發使用，未經授權請勿外傳。