using Microsoft.Extensions.Logging;
using Microsoft.FlightSimulator.SimConnect;
using System.Data;
using System.Runtime.InteropServices;
//using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using Button = System.Windows.Forms.Button;

namespace SimConnectTester
{
    public partial class SimConnectTester : Form
    {
        private SimConnect simConnect = null;
        private bool simConnectConnected = false;

        private readonly ILogger<SimConnectTester> _logger;
        // 定义SimConnect事件和变量

        private int totalLVARCount = 0;
        private int currentBatch = 0;
        private int totalBatches = 0;
        private List<string> allLVARs = new List<string>();
        private bool isGettingLVARList = false;

        enum ClientDataID
        {
            LVAR_REQUEST,
            LVAR_RESPONSE,
            LVAR_LIST_RESPONSE_ID  // 新增
        }
        enum DEFINITIONS
        {
            SimVarDefinition,
            SimEventDefinition,
            SIMVAR_DOUBLE,
            SIMVAR_STRING,
            LVAR_REQUEST_DEFINITION,  // 请求
            LVAR_RESPONSE_DEFINITION,  // 结果
            LVAR_LIST_RESPONSE_DEFINITION  // 新增
        }

        enum DATA_REQUESTS
        {
            RequestSimVar,
            RequestInputEvents,
            REQUEST_EnumerateInputEvents,
            REQUEST_LVAR_VALUE,  // LVAR Request
            RESPONSE_LVAR_VALUE,
            RESPONSE_LVAR_LIST,  // 新增
            RESPONSE_LVAR_LIST_COUNT
        }

        enum LVAR_EVENTS
        {
            EVENT_LVAR_READ,
            EVENT_LVAR_GOT
        }

        struct LVARRequestData
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string lvarName;
        }

        struct LVARResponseData
        {
            public double lvarValue;
        }

        // 新增数据结构
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
        struct LVARListResponseData
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 8192)]
            public string lvarList;
        }


        public enum SIMCONNECT_GROUP_PRIORITY : uint
        {
            HIGHEST = 1,
            HIGHEST_MASKABLE = 10000000,
            STANDARD = 1900000000,
            DEFAULT = 2000000000,
            LOWEST = 4000000000
        }
        struct Struct1
        {
            // this is how you declare a fixed size string
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public String sValue;

            // other definitions can be added to this struct
            // ...
        };

        enum FIXED_SIM_EVENTS
        {
            Event1,
            Event2,
            Event3,
            Event4,
            Event5,
            Event6,
            Event7,
            Event8,
            Event9,
            Event10
        }
        private static int event_index=0;

        //通知组
        enum GROUP_ID
        {
            GROUP_1
        };
        // 存储InputEvents
        //private List<string> inputEventsList = new List<string>();
        //private Dictionary<uint, string> inputEventIdToName = new Dictionary<uint, string>();
        private bool retrivedSimVar = false;
        private Dictionary<string, FIXED_SIM_EVENTS> eventNameToEnumMap = new Dictionary<string, FIXED_SIM_EVENTS>();
        private static Dictionary<string, object> simConnectInputEvents = new Dictionary<string, object>();
        private bool isInputEventsLoaded = false;
        private bool isGettingInputEvent = false;
        public struct InputEventFloatData
        {
            public double value_f64;
        };


        private GroupBox simVarGroupBox;
        private GroupBox simEventGroupBox;
        private GroupBox inputEventGroupBox;



        // SimVar控件
        private Label simVarNameLabel;
        private TextBox simVarNameTextBox;
        private Label simVarTypeLabel;
        private TextBox simVarTypeTextBox;
        private Label simVarValueLabel;
        private TextBox simVarValueTextBox;
        private Button simVarGetButton;
        private Button simVarSetButton;
        private Label simVarResultLabel;

        // SimEvent控件
        private Label simEventNameLabel;
        private TextBox simEventNameTextBox;
        private Label simEventTypeLabel;
        private TextBox simEventTypeTextBox;
        private Label simEventValueLabel;
        private TextBox simEventValueTextBox;
        private Button simEventTriggerButton;
        private Label simEventResultLabel;

        // InputEvent控件
        private Label inputEventLabel;
        private ComboBox inputEventComboBox;
        private Label inputEventValueLabel;
        private TextBox inputEventValueTextBox;
        private Button inputEventGetButton;
        private Button inputEventTriggerButton;
        private Label inputEventResultLabel;

        //LVAR 部分
        private GroupBox lvarGroupBox;
        private Label lvarNameLabel;
        private TextBox lvarNameTextBox;
        private Button lvarGetButton;
        private Label lvarResultLabel;


        // 状态标签
        private Label statusLabel;
        public SimConnectTester()
        {
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.AddDebug();
                //builder.SetMinimumLevel(LogLevel.Information);
                builder.SetMinimumLevel(LogLevel.Debug);

            });
            _logger = loggerFactory.CreateLogger<SimConnectTester>();
            InitializeComponent();
            InitializeSimConnect();
        }

        private void InitializeSimConnect()
        {
            try
            {
                simConnect = new SimConnect("SimConnectTester", this.Handle, 0x402, null, 0);

                // 注册SimConnect事件处理
                simConnect.OnRecvOpen += new SimConnect.RecvOpenEventHandler(SimConnect_OnRecvOpen);
                simConnect.OnRecvQuit += new SimConnect.RecvQuitEventHandler(SimConnect_OnRecvQuit);
                simConnect.OnRecvException += new SimConnect.RecvExceptionEventHandler(SimConnect_OnRecvException);
                simConnect.OnRecvSimobjectData += new SimConnect.RecvSimobjectDataEventHandler(SimConnect_OnRecvSimobjectData);
                simConnect.OnRecvEnumerateInputEvents += OnRecvEventEnum;
                simConnect.OnRecvEnumerateInputEventParams += OnRecvEventEnumParams;
                simConnect.OnRecvGetInputEvent += OnRecvGetInputEvent;
                simConnect.OnRecvEvent += new SimConnect.RecvEventEventHandler(SimConnect_OnRecvEvent);
                simConnect.OnRecvClientData += new SimConnect.RecvClientDataEventHandler(SimConnect_OnRecvClientData);

                simConnectConnected = true;
                UpdateStatus("SimConnect连接成功");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"SimConnect初始化失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                simConnectConnected = false;
                UpdateStatus("SimConnect连接失败");
            }
        }

        private void InitializeComponent()
        {
            //this.components = new System.ComponentModel.Container();
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(600, 900);
            this.Text = "Flight Simulator Controller";
            this.StartPosition = FormStartPosition.CenterScreen;

            InitializeSimVarSection();
            InitializeSimEventSection();
            InitializeInputEventSection();
            InitializeLVARSection();
            InitializeStatusLabel();

            // 设置Tab顺序
            SetTabOrder();
        }

        private void InitializeSimVarSection()
        {
            simVarGroupBox = new GroupBox();
            simVarGroupBox.Text = "SimVar";
            simVarGroupBox.Location = new Point(20, 20);
            simVarGroupBox.Size = new Size(560, 180);
            this.Controls.Add(simVarGroupBox);

            // SimVar Name
            simVarNameLabel = new Label();
            simVarNameLabel.Text = "Name:";
            simVarNameLabel.Location = new Point(20, 30);
            simVarNameLabel.Size = new Size(80, 20);
            simVarGroupBox.Controls.Add(simVarNameLabel);

            simVarNameTextBox = new TextBox();
            simVarNameTextBox.Location = new Point(100, 27);
            simVarNameTextBox.Size = new Size(200, 20);
            simVarGroupBox.Controls.Add(simVarNameTextBox);

            // SimVar Type
            simVarTypeLabel = new Label();
            simVarTypeLabel.Text = "Type:";
            simVarTypeLabel.Location = new Point(20, 60);
            simVarTypeLabel.Size = new Size(80, 20);
            simVarGroupBox.Controls.Add(simVarTypeLabel);

            simVarTypeTextBox = new TextBox();
            simVarTypeTextBox.Location = new Point(100, 57);
            simVarTypeTextBox.Size = new Size(200, 20);
            simVarGroupBox.Controls.Add(simVarTypeTextBox);

            // SimVar Value
            simVarValueLabel = new Label();
            simVarValueLabel.Text = "Value:";
            simVarValueLabel.Location = new Point(20, 90);
            simVarValueLabel.Size = new Size(80, 20);
            simVarGroupBox.Controls.Add(simVarValueLabel);

            simVarValueTextBox = new TextBox();
            simVarValueTextBox.Location = new Point(100, 87);
            simVarValueTextBox.Size = new Size(200, 20);
            simVarGroupBox.Controls.Add(simVarValueTextBox);

            // 按钮
            simVarGetButton = new Button();
            simVarGetButton.Text = "获取";
            simVarGetButton.Location = new Point(320, 27);
            simVarGetButton.Size = new Size(80, 30);
            simVarGetButton.Click += simVarGetButton_Click;
            simVarGroupBox.Controls.Add(simVarGetButton);

            simVarSetButton = new Button();
            simVarSetButton.Text = "修改";
            simVarSetButton.Location = new Point(320, 67);
            simVarSetButton.Size = new Size(80, 30);
            simVarSetButton.Click += simVarSetButton_Click;
            simVarGroupBox.Controls.Add(simVarSetButton);

            // 结果显示
            simVarResultLabel = new Label();
            simVarResultLabel.Text = "结果将显示在这里...";
            simVarResultLabel.Location = new Point(20, 130);
            simVarResultLabel.Size = new Size(520, 40);
            simVarResultLabel.BorderStyle = BorderStyle.FixedSingle;
            simVarResultLabel.BackColor = Color.LightGray;
            simVarGroupBox.Controls.Add(simVarResultLabel);
        }

        private void InitializeSimEventSection()
        {
            simEventGroupBox = new GroupBox();
            simEventGroupBox.Text = "SimEvent";
            simEventGroupBox.Location = new Point(20, 220);
            simEventGroupBox.Size = new Size(560, 180);
            this.Controls.Add(simEventGroupBox);

            // SimEvent Name
            simEventNameLabel = new Label();
            simEventNameLabel.Text = "Name:";
            simEventNameLabel.Location = new Point(20, 30);
            simEventNameLabel.Size = new Size(80, 20);
            simEventGroupBox.Controls.Add(simEventNameLabel);

            simEventNameTextBox = new TextBox();
            simEventNameTextBox.Location = new Point(100, 27);
            simEventNameTextBox.Size = new Size(200, 20);
            simEventGroupBox.Controls.Add(simEventNameTextBox);

            // SimEvent Type
            simEventTypeLabel = new Label();
            simEventTypeLabel.Text = "Type:";
            simEventTypeLabel.Location = new Point(20, 60);
            simEventTypeLabel.Size = new Size(80, 20);
            simEventGroupBox.Controls.Add(simEventTypeLabel);

            simEventTypeTextBox = new TextBox();
            simEventTypeTextBox.Location = new Point(100, 57);
            simEventTypeTextBox.Size = new Size(200, 20);
            simEventGroupBox.Controls.Add(simEventTypeTextBox);

            // SimEvent Value
            simEventValueLabel = new Label();
            simEventValueLabel.Text = "Value:";
            simEventValueLabel.Location = new Point(20, 90);
            simEventValueLabel.Size = new Size(80, 20);
            simEventGroupBox.Controls.Add(simEventValueLabel);

            simEventValueTextBox = new TextBox();
            simEventValueTextBox.Location = new Point(100, 87);
            simEventValueTextBox.Size = new Size(200, 20);
            simEventGroupBox.Controls.Add(simEventValueTextBox);

            // 触发按钮
            simEventTriggerButton = new Button();
            simEventTriggerButton.Text = "触发";
            simEventTriggerButton.Location = new Point(320, 47);
            simEventTriggerButton.Size = new Size(80, 30);
            simEventTriggerButton.Click += simEventTriggerButton_Click;
            simEventGroupBox.Controls.Add(simEventTriggerButton);

            // 结果显示
            simEventResultLabel = new Label();
            simEventResultLabel.Text = "结果将显示在这里...";
            simEventResultLabel.Location = new Point(20, 130);
            simEventResultLabel.Size = new Size(520, 40);
            simEventResultLabel.BorderStyle = BorderStyle.FixedSingle;
            simEventResultLabel.BackColor = Color.LightGray;
            simEventGroupBox.Controls.Add(simEventResultLabel);
        }

        private void InitializeInputEventSection()
        {
            inputEventGroupBox = new GroupBox();
            inputEventGroupBox.Text = "InputEvent";
            inputEventGroupBox.Location = new Point(20, 420);
            inputEventGroupBox.Size = new Size(560, 180);
            this.Controls.Add(inputEventGroupBox);

            // InputEvent 下拉列表
            inputEventLabel = new Label();
            inputEventLabel.Text = "Event:";
            inputEventLabel.Location = new Point(20, 35);
            inputEventLabel.Size = new Size(80, 20);
            inputEventGroupBox.Controls.Add(inputEventLabel);

            inputEventComboBox = new ComboBox();
            inputEventComboBox.Location = new Point(100, 32);
            inputEventComboBox.Size = new Size(200, 21);
            inputEventComboBox.DropDownStyle = ComboBoxStyle.DropDown;
            inputEventComboBox.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            inputEventComboBox.AutoCompleteSource = AutoCompleteSource.ListItems;
            inputEventComboBox.KeyUp += inputEventComboBox_KeyUp;
            inputEventGroupBox.Controls.Add(inputEventComboBox);

            // InputEvent Value
            inputEventValueLabel = new Label();
            inputEventValueLabel.Text = "Value:";
            inputEventValueLabel.Location = new Point(20, 70);
            inputEventValueLabel.Size = new Size(80, 20);
            inputEventGroupBox.Controls.Add(inputEventValueLabel);

            inputEventValueTextBox = new TextBox();
            inputEventValueTextBox.Location = new Point(100, 67);
            inputEventValueTextBox.Size = new Size(200, 20);
            inputEventGroupBox.Controls.Add(inputEventValueTextBox);

            // 按钮
            inputEventGetButton = new Button();
            inputEventGetButton.Text = "获取值";
            inputEventGetButton.Location = new Point(320, 30);
            inputEventGetButton.Size = new Size(80, 30);
            inputEventGetButton.Click += inputEventGetButton_Click;
            inputEventGroupBox.Controls.Add(inputEventGetButton);

            inputEventTriggerButton = new Button();
            inputEventTriggerButton.Text = "设置值";
            inputEventTriggerButton.Location = new Point(320, 65);
            inputEventTriggerButton.Size = new Size(80, 30);
            inputEventTriggerButton.Click += inputEventTriggerButton_Click;
            inputEventGroupBox.Controls.Add(inputEventTriggerButton);

            // 结果显示
            inputEventResultLabel = new Label();
            inputEventResultLabel.Text = "结果将显示在这里...";
            inputEventResultLabel.Location = new Point(20, 110);
            inputEventResultLabel.Size = new Size(520, 40);
            inputEventResultLabel.BorderStyle = BorderStyle.FixedSingle;
            inputEventResultLabel.BackColor = Color.LightGray;
            inputEventGroupBox.Controls.Add(inputEventResultLabel);
        }
        private void InitializeLVARSection()
        {
            lvarGroupBox = new GroupBox();
            lvarGroupBox.Text = "LVAR";
            lvarGroupBox.Location = new Point(20, 620);
            lvarGroupBox.Size = new Size(560, 150);
            this.Controls.Add(lvarGroupBox);

            // LVAR 下拉列表
            Label lvarSelectLabel = new Label();
            lvarSelectLabel.Text = "选择LVAR:";
            lvarSelectLabel.Location = new Point(20, 30);
            lvarSelectLabel.Size = new Size(80, 20);
            lvarGroupBox.Controls.Add(lvarSelectLabel);

            ComboBox lvarComboBox = new ComboBox();
            lvarComboBox.Name = "lvarComboBox";
            lvarComboBox.Location = new Point(100, 27);
            lvarComboBox.Size = new Size(300, 21);
            lvarComboBox.DropDownStyle = ComboBoxStyle.DropDown;
            lvarComboBox.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            lvarComboBox.AutoCompleteSource = AutoCompleteSource.ListItems;
            lvarGroupBox.Controls.Add(lvarComboBox);

            // LVAR Name
            lvarNameLabel = new Label();
            lvarNameLabel.Text = "LVAR Name:";
            lvarNameLabel.Location = new Point(20, 60);
            lvarNameLabel.Size = new Size(80, 20);
            lvarGroupBox.Controls.Add(lvarNameLabel);

            lvarNameTextBox = new TextBox();
            lvarNameTextBox.Location = new Point(100, 57);
            lvarNameTextBox.Size = new Size(300, 20);
            lvarGroupBox.Controls.Add(lvarNameTextBox);

            // 获取按钮
            lvarGetButton = new Button();
            lvarGetButton.Text = "获取LVAR值";
            lvarGetButton.Location = new Point(420, 57);
            lvarGetButton.Size = new Size(100, 30);
            lvarGetButton.Click += lvarGetButton_Click;
            lvarGroupBox.Controls.Add(lvarGetButton);

            // 刷新列表按钮
            Button refreshLvarListButton = new Button();
            refreshLvarListButton.Text = "刷新列表";
            refreshLvarListButton.Location = new Point(420, 27);
            refreshLvarListButton.Size = new Size(100, 30);
            refreshLvarListButton.Click += RefreshLvarListButton_Click;
            lvarGroupBox.Controls.Add(refreshLvarListButton);

            // 结果显示
            lvarResultLabel = new Label();
            lvarResultLabel.Text = "LVAR值将显示在这里...";
            lvarResultLabel.Location = new Point(20, 100);
            lvarResultLabel.Size = new Size(520, 60);
            lvarResultLabel.BorderStyle = BorderStyle.FixedSingle;
            lvarResultLabel.BackColor = Color.LightGray;
            lvarGroupBox.Controls.Add(lvarResultLabel);
        }
        private void InitializeStatusLabel()
        {
            statusLabel = new Label();
            statusLabel.Text = "状态: 未连接";
            statusLabel.Location = new Point(20, 620);
            statusLabel.Size = new Size(560, 20);
            statusLabel.BorderStyle = BorderStyle.FixedSingle;
            statusLabel.BackColor = SystemColors.Info;
            this.Controls.Add(statusLabel);
        }

        private void SetTabOrder()
        {
            // 设置Tab键顺序
            simVarNameTextBox.TabIndex = 0;
            simVarTypeTextBox.TabIndex = 1;
            simVarValueTextBox.TabIndex = 2;
            simVarGetButton.TabIndex = 3;
            simVarSetButton.TabIndex = 4;

            simEventNameTextBox.TabIndex = 5;
            simEventTypeTextBox.TabIndex = 6;
            simEventValueTextBox.TabIndex = 7;
            simEventTriggerButton.TabIndex = 8;

            inputEventComboBox.TabIndex = 9;
            inputEventValueTextBox.TabIndex = 10;
            inputEventGetButton.TabIndex = 11;
            inputEventTriggerButton.TabIndex = 12;

            lvarNameTextBox.TabIndex = 13;  // 新增
            lvarGetButton.TabIndex = 14;    // 新增
        }
        #region SimConnect事件处理
        private async void SimConnect_OnRecvOpen(SimConnect sender, SIMCONNECT_RECV_OPEN data)
        {
            UpdateStatus("已连接到Microsoft Flight Simulator");
            
            await EnumerateInputEvents();
            // 设置请求ClientData区域
            // 定义请求数据结构
            simConnect.MapClientDataNameToID("CVCWASMDATA_REQUEST", ClientDataID.LVAR_REQUEST);
            simConnect.CreateClientData(ClientDataID.LVAR_REQUEST, 256, SIMCONNECT_CREATE_CLIENT_DATA_FLAG.DEFAULT);
            simConnect.AddToClientDataDefinition(DEFINITIONS.LVAR_REQUEST_DEFINITION, 0, 256, 0, 0);
            simConnect.RegisterStruct<SIMCONNECT_RECV_CLIENT_DATA, LVARRequestData>(DEFINITIONS.LVAR_REQUEST_DEFINITION);

            // 定义响应数据结构
            simConnect.MapClientDataNameToID("CVCWASMDATA_RESPONSE", ClientDataID.LVAR_RESPONSE);
            simConnect.CreateClientData(ClientDataID.LVAR_RESPONSE, 8, SIMCONNECT_CREATE_CLIENT_DATA_FLAG.DEFAULT);
            simConnect.AddToClientDataDefinition(DEFINITIONS.LVAR_RESPONSE_DEFINITION, 0, 8, 0, 0);
            simConnect.RegisterStruct<SIMCONNECT_RECV_CLIENT_DATA, LVARResponseData>(DEFINITIONS.LVAR_RESPONSE_DEFINITION);

            // 新增：LVAR列表响应ClientData
            simConnect.MapClientDataNameToID("CVCWASMDATA_LIST_RESPONSE", ClientDataID.LVAR_LIST_RESPONSE_ID);
            simConnect.CreateClientData(ClientDataID.LVAR_LIST_RESPONSE_ID, 4096, SIMCONNECT_CREATE_CLIENT_DATA_FLAG.DEFAULT);
            simConnect.AddToClientDataDefinition(DEFINITIONS.LVAR_LIST_RESPONSE_DEFINITION, 0, 4096, 0, 0);
            simConnect.RegisterStruct<SIMCONNECT_RECV_CLIENT_DATA, LVARListResponseData>(DEFINITIONS.LVAR_LIST_RESPONSE_DEFINITION);

            // 映射事件
            /*
            simConnect.MapClientEventToSimEvent(LVAR_EVENTS.EVENT_LVAR_READ, "CVC.LVARREAD");
            simConnect.MapClientEventToSimEvent(LVAR_EVENTS.EVENT_LVAR_GOT, "CVC.LVARGOT");

            // 订阅响应事件
            simConnect.AddClientEventToNotificationGroup(GROUP_ID.GROUP_1, LVAR_EVENTS.EVENT_LVAR_GOT, false);
            */
            // 请求LVAR列表
            RequestLVARList();
        }

        private void RequestLVARList()
        {
            if (isGettingLVARList) return;
            try
            {
                isGettingLVARList = true;
                allLVARs.Clear();
                currentBatch = 0;
                totalBatches = 0;

                // 首先请求LVAR总数
                LVARRequestData requestData = new LVARRequestData { lvarName = "WASM.GetLVARListCount" };
                simConnect.SetClientData(ClientDataID.LVAR_REQUEST, DEFINITIONS.LVAR_REQUEST_DEFINITION,
                    SIMCONNECT_CLIENT_DATA_SET_FLAG.DEFAULT, 0, requestData);

                // 订阅总数响应
                simConnect.RequestClientData(ClientDataID.LVAR_RESPONSE, DATA_REQUESTS.RESPONSE_LVAR_VALUE,
                    DEFINITIONS.LVAR_RESPONSE_DEFINITION, SIMCONNECT_CLIENT_DATA_PERIOD.ONCE,
                    SIMCONNECT_CLIENT_DATA_REQUEST_FLAG.DEFAULT, 0, 0, 0);

                UpdateLVARResult("正在获取LVAR总数...");

                /*
                // 发送获取LVAR列表的请求
                LVARRequestData requestData = new LVARRequestData { lvarName = "WASM.GetLVARList" };
                simConnect.SetClientData(ClientDataID.LVAR_REQUEST, DEFINITIONS.LVAR_REQUEST_DEFINITION,
                    SIMCONNECT_CLIENT_DATA_SET_FLAG.DEFAULT, 0, requestData);

                // 订阅LVAR列表响应
                simConnect.RequestClientData(ClientDataID.LVAR_LIST_RESPONSE_ID, DATA_REQUESTS.RESPONSE_LVAR_LIST,
                    DEFINITIONS.LVAR_LIST_RESPONSE_DEFINITION, SIMCONNECT_CLIENT_DATA_PERIOD.ON_SET,
                    SIMCONNECT_CLIENT_DATA_REQUEST_FLAG.DEFAULT, 0, 0, 0);

                UpdateLVARResult("正在获取LVAR列表...");
                */
            }
            catch (Exception ex)
            {
                isGettingLVARList = false;
                UpdateLVARResult($"获取LVAR列表失败: {ex.Message}");
            }
        }
        private void RequestLVARBatch(int batch)
        {
            try
            {
                // 发送批次请求
                LVARRequestData requestData = new LVARRequestData { lvarName = $"WASM.GetLVARList.{batch}" };
                simConnect.SetClientData(ClientDataID.LVAR_REQUEST, DEFINITIONS.LVAR_REQUEST_DEFINITION,
                    SIMCONNECT_CLIENT_DATA_SET_FLAG.DEFAULT, 0, requestData);

                // 订阅列表响应
                simConnect.RequestClientData(ClientDataID.LVAR_LIST_RESPONSE_ID, DATA_REQUESTS.RESPONSE_LVAR_LIST,
                    DEFINITIONS.LVAR_LIST_RESPONSE_DEFINITION, SIMCONNECT_CLIENT_DATA_PERIOD.ONCE,
                    SIMCONNECT_CLIENT_DATA_REQUEST_FLAG.DEFAULT, 0, 0, 0);

                UpdateLVARResult($"正在获取LVAR批次 {batch}/{totalBatches}...");
            }
            catch (Exception ex)
            {
                UpdateLVARResult($"获取LVAR批次 {batch} 失败: {ex.Message}");
            }
        }

        private void SimConnect_OnRecvQuit(SimConnect sender, SIMCONNECT_RECV data)
        {
            UpdateStatus("Flight Simulator已断开连接");
            simConnect.ClearClientDataDefinition(DEFINITIONS.LVAR_REQUEST_DEFINITION);
            simConnect.ClearClientDataDefinition(DEFINITIONS.LVAR_RESPONSE_DEFINITION);
            simConnect.ClearNotificationGroup(GROUP_ID.GROUP_1);
            simConnectConnected = false;
        }

        private void SimConnect_OnRecvException(SimConnect sender, SIMCONNECT_RECV_EXCEPTION data)
        {
            UpdateStatus($"SimConnect异常: {data.dwException}");
        }

        private void SimConnect_OnRecvSimobjectData(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA data)
        {
            _logger.LogDebug("In SimConnect_OnRecvSimobjectData ");
            switch ((DATA_REQUESTS)data.dwRequestID)
            {
                case DATA_REQUESTS.RequestSimVar:
                    // 处理SimVar数据返回
                    double simVarValue = (double)data.dwData[0];
                    retrivedSimVar = true;
                    UpdateSimVarResult($"获取成功: {simVarValue}");
                    _logger.LogDebug($"获取成功: {simVarValue}");
                    break;
                /*
                case DATA_REQUESTS.REQUEST_LVAR_VALUE:
                    // 处理LVAR数据返回
                    LVARResponseData responseData = (LVARResponseData)data.dwData[0];
                    UpdateLVARResult($"LVAR值: {responseData.lvarValue}");
                    break;
                */
            }
        }

        // 新增事件处理
        private void SimConnect_OnRecvEvent(SimConnect sender, SIMCONNECT_RECV_EVENT data)
        {
            /*
            switch ((LVAR_EVENTS)data.uEventID)
            {
                case LVAR_EVENTS.EVENT_LVAR_GOT:
                    // 请求响应数据
                    simConnect.RequestClientData(ClientDataID.LVAR_RESPONSE, DATA_REQUESTS.REQUEST_LVAR_VALUE,
                        DEFINITIONS.LVAR_RESPONSE_DEFINITION, SIMCONNECT_CLIENT_DATA_PERIOD.ONCE, SIMCONNECT_CLIENT_DATA_REQUEST_FLAG.DEFAULT,0,0,0);
                    break;
            }
            */
        }

        private void SimConnect_OnRecvClientData(SimConnect sender, SIMCONNECT_RECV_CLIENT_DATA data)
        {
            _logger.LogDebug($"In SimConnect_OnRecvClientData->data.dwRequestID:{data.dwRequestID}");
            switch ((DATA_REQUESTS)data.dwRequestID)
            {
                case DATA_REQUESTS.RESPONSE_LVAR_VALUE:
                    // 处理LVAR数据返回
                    LVARResponseData responseData = (LVARResponseData)data.dwData[0];
                    UpdateLVARResult($"LVAR值: {responseData.lvarValue} 返回数：{data.dwoutof}");
                    break;

                case DATA_REQUESTS.RESPONSE_LVAR_LIST_COUNT:
                    // 处理LVAR总数响应
                    LVARResponseData listcountData = (LVARResponseData)data.dwData[0];
                    totalLVARCount = (int)listcountData.lvarValue;
                    totalBatches = (totalLVARCount + 99) / 100; // 计算总批次数

                    UpdateLVARResult($"找到 {totalLVARCount} 个LVAR，共 {totalBatches} 批");

                    if (totalBatches > 0)
                    {
                        // 开始请求第一批
                        currentBatch = 1;
                        RequestLVARBatch(currentBatch);
                    }
                    else
                    {
                        isGettingLVARList = false;
                        UpdateLVARList("[]"); // 空列表
                    }
                    break;

                case DATA_REQUESTS.RESPONSE_LVAR_LIST:
                    // 处理LVAR列表响应
                    LVARListResponseData listResponse = (LVARListResponseData)data.dwData[0];
                    try
                    {
                        // 解析当前批次的LVAR列表
                        var batchLVARs = System.Text.Json.JsonSerializer.Deserialize<string[]>(listResponse.lvarList);
                        if (batchLVARs != null)
                        {
                            allLVARs.AddRange(batchLVARs);
                        }

                        // 检查是否还有更多批次
                        currentBatch++;
                        if (currentBatch <= totalBatches)
                        {
                            // 请求下一批
                            RequestLVARBatch(currentBatch);
                        }
                        else
                        {
                            // 所有批次完成，更新UI
                            UpdateLVARListFromCollection(allLVARs);
                            isGettingLVARList = false;
                        }
                    }
                    catch (Exception ex)
                    {
                        UpdateLVARResult($"解析LVAR批次失败: {ex.Message}");
                        isGettingLVARList = false;
                    }
                    break;
            }
        }
        #endregion

        #region UI更新方法
        private void UpdateStatus(string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(UpdateStatus), message);
                return;
            }
            statusLabel.Text = $"状态: {message}";
        }

        private void UpdateSimVarResult(string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(UpdateSimVarResult), message);
                return;
            }
            simVarResultLabel.Text = message;
        }

        private void UpdateInputEventResult(string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(UpdateInputEventResult), message);
                return;
            }
            inputEventResultLabel.Text = message;
        }
        private void UpdateLVARResult(string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(UpdateLVARResult), message);
                return;
            }
            lvarResultLabel.Text = message;
        }

        // 新增方法：更新LVAR下拉列表
        private void UpdateLVARList(string lvarListJson)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(UpdateLVARList), lvarListJson);
                return;
            }

            try
            {
                // 解析JSON数组
                var lvarNames = System.Text.Json.JsonSerializer.Deserialize<string[]>(lvarListJson);

                // 查找LVAR下拉列表控件
                ComboBox lvarComboBox = lvarGroupBox.Controls.Find("lvarComboBox", true).FirstOrDefault() as ComboBox;

                if (lvarComboBox != null && lvarNames != null)
                {
                    lvarComboBox.BeginUpdate();
                    lvarComboBox.Items.Clear();
                    foreach (var lvarName in lvarNames)
                    {
                        lvarComboBox.Items.Add(lvarName);
                    }
                    lvarComboBox.EndUpdate();

                    UpdateLVARResult($"LVAR列表已更新，共 {lvarNames.Length} 个变量");
                }
            }
            catch (Exception ex)
            {
                UpdateLVARResult($"解析LVAR列表失败: {ex.Message}");
            }
        }
        private void UpdateLVARListFromCollection(List<string> lvarNames)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<List<string>>(UpdateLVARListFromCollection), lvarNames);
                return;
            }

            try
            {
                ComboBox lvarComboBox = lvarGroupBox.Controls.Find("lvarComboBox", true).FirstOrDefault() as ComboBox;
                if (lvarComboBox != null)
                {
                    lvarComboBox.BeginUpdate();
                    lvarComboBox.Items.Clear();
                    foreach (var lvarName in lvarNames)
                    {
                        lvarComboBox.Items.Add(lvarName);
                    }
                    lvarComboBox.EndUpdate();
                    UpdateLVARResult($"LVAR列表已更新，共 {lvarNames.Count} 个变量");
                }
            }
            catch (Exception ex)
            {
                UpdateLVARResult($"更新LVAR列表失败: {ex.Message}");
            }
        }
        #endregion

        #region SimVar功能
        private async void simVarGetButton_Click(object sender, EventArgs e)
        {
            if (!simConnectConnected)
            {
                MessageBox.Show("SimConnect未连接", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string name = simVarNameTextBox.Text.Trim();
            string type = simVarTypeTextBox.Text.Trim();
            string value = simVarValueTextBox.Text.Trim();

            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(type))
            {
                MessageBox.Show("Name和Type不能为空", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                simConnect.ClearDataDefinition(DEFINITIONS.SIMVAR_DOUBLE);
                simConnect.ClearDataDefinition(DEFINITIONS.SIMVAR_STRING);
                // 定义SimVar
                if (type != "String")
                {
                    simConnect.AddToDataDefinition(DEFINITIONS.SIMVAR_DOUBLE, name, type, SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                    simConnect.RegisterDataDefineStruct<double>(DEFINITIONS.SIMVAR_DOUBLE);
                    // 请求SimVar数据
                    simConnect.RequestDataOnSimObject(DATA_REQUESTS.RequestSimVar, DEFINITIONS.SIMVAR_DOUBLE, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.ONCE, 0, 0, 0, 0);
                }
                else
                {
                    simConnect.AddToDataDefinition(DEFINITIONS.SIMVAR_STRING, name, null, SIMCONNECT_DATATYPE.STRING256, 0, SimConnect.SIMCONNECT_UNUSED);
                    simConnect.RegisterDataDefineStruct<Struct1>(DEFINITIONS.SIMVAR_STRING);
                    // 请求SimVar数据
                    simConnect.RequestDataOnSimObject(DATA_REQUESTS.RequestSimVar, DEFINITIONS.SIMVAR_STRING, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.ONCE, 0, 0, 0, 0);
                }
                await Task.Delay(300);
                simConnect.ReceiveMessage();
                //循环尝试，直至Connect_Wait_Time定义的尝试次数。直到取得数据或者超时 
                int loop = 0;
                bool running = true;
                _logger.LogDebug($"GetSimVarValueAsync:等待获取SimVar数据，第{loop}次...");
                while (running && !retrivedSimVar)
                {
                    loop++;
                    _logger.LogDebug($"GetSimVarValueAsync:等待获取SimVar数据，第{loop}次...");

                    if (simConnect != null)
                    {
                        // 获取一次数据
                        simConnect.ReceiveMessage();

                    }

                    //debug
                    //if (loop > 60000)
                    if (loop > 10)
                    {
                        running = false;
                        _logger.LogWarning("获取SimVar值超时");
                        return;
                    }

                    Thread.Sleep(500); // 短暂休眠以避免CPU过度使用
                    //Debug
                    //Thread.Sleep(1000); // 短暂休眠以避免CPU过度使用
                    UpdateSimVarResult("正在获取SimVar...");
                }
                
            }
            catch (Exception ex)
            {
                UpdateSimVarResult($"获取失败: {ex.Message}");
                _logger.LogDebug($"获取失败: {ex.Message}");
            }
        }

        private void simVarSetButton_Click(object sender, EventArgs e)
        {
            if (!simConnectConnected)
            {
                MessageBox.Show("SimConnect未连接", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string name = simVarNameTextBox.Text.Trim();
            string type = simVarTypeTextBox.Text.Trim();
            string value = simVarValueTextBox.Text.Trim();

            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(type) || string.IsNullOrEmpty(value))
            {
                MessageBox.Show("Name、Type和Value都不能为空", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                simConnect.ClearDataDefinition(DEFINITIONS.SIMVAR_DOUBLE);
                simConnect.ClearDataDefinition(DEFINITIONS.SIMVAR_STRING);
                // 定义SimVar
                if (type != "String")
                {
                    if (double.TryParse(value, out double doubleValue))
                    {
                        _logger.LogDebug($"doubleValue:{doubleValue}");
                        simConnect.AddToDataDefinition(DEFINITIONS.SIMVAR_DOUBLE, name, type, SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                        simConnect.RegisterDataDefineStruct<double>(DEFINITIONS.SIMVAR_DOUBLE);
                        // 请求SimVar数据
                        //simConnect.RequestDataOnSimObject(DATA_REQUESTS.RequestSimVar, DEFINITIONS.SIMVAR_DOUBLE, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.ONCE, 0, 0, 0, 0);
                        // 设置SimVar值
                        simConnect.SetDataOnSimObject(DEFINITIONS.SIMVAR_DOUBLE, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_DATA_SET_FLAG.DEFAULT,  doubleValue );
                    }
                    else
                    {
                        UpdateSimVarResult("Value必须是有效的数字");
                    }
                }
                else
                {
                    simConnect.AddToDataDefinition(DEFINITIONS.SIMVAR_STRING, name, null, SIMCONNECT_DATATYPE.STRING256,0, SimConnect.SIMCONNECT_UNUSED);
                    simConnect.RegisterDataDefineStruct<Struct1>(DEFINITIONS.SIMVAR_STRING);
                    // 请求SimVar数据
                    simConnect.RequestDataOnSimObject(DATA_REQUESTS.RequestSimVar, DEFINITIONS.SIMVAR_STRING, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.ONCE, 0, 0, 0, 0);
                    simConnect.SetDataOnSimObject(DEFINITIONS.SIMVAR_STRING, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_DATA_SET_FLAG.DEFAULT, value );
                }
                simConnect.ReceiveMessage();
                UpdateSimVarResult($"设置成功: {value}");
                

            }
            catch (Exception ex)
            {
                UpdateSimVarResult($"设置失败: {ex.Message}");
            }
        }
        #endregion

        #region SimEvent功能
        private void simEventTriggerButton_Click(object sender, EventArgs e)
        {
            if (!simConnectConnected)
            {
                MessageBox.Show("SimConnect未连接", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string name = simEventNameTextBox.Text.Trim();
            string value = simEventValueTextBox.Text.Trim();

            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Event Name不能为空", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                FIXED_SIM_EVENTS currentEvent;

                // 检查是否已经处理过该事件名
                if (eventNameToEnumMap.ContainsKey(name))
                {
                    // 如果已存在，使用之前映射的枚举值
                    currentEvent = eventNameToEnumMap[name];
                }
                else
                {
                    // 如果不存在，创建新的映射
                    if (event_index > 9)
                    {
                        event_index = 0;
                        simConnect.ClearNotificationGroup(GROUP_ID.GROUP_1);
                        // 清空映射字典，因为通知组已清除
                        eventNameToEnumMap.Clear();
                    }

                    currentEvent = (FIXED_SIM_EVENTS)event_index;
                    simConnect.MapClientEventToSimEvent(currentEvent, name);
                    simConnect.AddClientEventToNotificationGroup(GROUP_ID.GROUP_1, currentEvent, false);

                    // 保存映射关系
                    eventNameToEnumMap[name] = currentEvent;
                    event_index++;
                }

                // 触发事件
                if (!string.IsNullOrEmpty(value) && double.TryParse(value, out double eventValue))
                {
                    simConnect.TransmitClientEvent(SimConnect.SIMCONNECT_OBJECT_ID_USER, currentEvent, (uint)eventValue, SIMCONNECT_GROUP_PRIORITY.HIGHEST, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                }
                else
                {
                    simConnect.TransmitClientEvent(SimConnect.SIMCONNECT_OBJECT_ID_USER, currentEvent, 0, SIMCONNECT_GROUP_PRIORITY.HIGHEST, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                }

                simEventResultLabel.Text = $"事件触发成功: {name}";

                /*
                if (event_index > 9)
                {
                    event_index = 0;
                    simConnect.ClearNotificationGroup(GROUP_ID.GROUP_1);
                }

                simConnect.MapClientEventToSimEvent((FIXED_SIM_EVENTS)event_index, name);
                simConnect.AddClientEventToNotificationGroup(GROUP_ID.GROUP_1, (FIXED_SIM_EVENTS)event_index, false);



                if (!string.IsNullOrEmpty(value) && double.TryParse(value, out double eventValue))
                {
                    simConnect.TransmitClientEvent(SimConnect.SIMCONNECT_OBJECT_ID_USER, (FIXED_SIM_EVENTS)event_index, (uint)eventValue, SIMCONNECT_GROUP_PRIORITY.HIGHEST, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                }
                else
                {
                    simConnect.TransmitClientEvent(SimConnect.SIMCONNECT_OBJECT_ID_USER, (FIXED_SIM_EVENTS)event_index, 0, SIMCONNECT_GROUP_PRIORITY.HIGHEST, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                }
                event_index++;
                simEventResultLabel.Text = $"事件触发成功: {name}";
                */
            }
            catch (Exception ex)
            {
                simEventResultLabel.Text = $"事件触发失败: {ex.Message}";
            }
        }
     
        #endregion

        #region InputEvent功能
        private void OnRecvEventEnum(SimConnect sender, SIMCONNECT_RECV_ENUMERATE_INPUT_EVENTS data)
        {
            if (!isInputEventsLoaded)
            {
                for (int i = 0; i < data.dwArraySize; ++i)
                {
                    SIMCONNECT_INPUT_EVENT_DESCRIPTOR msg = (SIMCONNECT_INPUT_EVENT_DESCRIPTOR)data.rgData[i];

                    _logger.LogDebug($"Input Event Found:");
                    _logger.LogDebug($"  InputEvent ID: {msg.Hash}");
                    _logger.LogDebug($"  InputEvent eType: {msg.eType}");
                    _logger.LogDebug($"  InputEvent Name: {msg.Name}");

                    if(!simConnectInputEvents.Keys.Contains(msg.Name))
                    { 
                        simConnectInputEvents.Add(msg.Name, msg.Hash);
                        inputEventComboBox.Items.Add(msg.Name);
                        simConnect.EnumerateInputEventParams(msg.Hash);
                    }
                }
                if (simConnectInputEvents.Count > 1)
                {
                    isInputEventsLoaded = true;
                    _logger.LogDebug($"In:OnRecvEventEnum isInputEventsLoaded is set:{isInputEventsLoaded}");
                }
            }
        }

        private void OnRecvEventEnumParams(SimConnect sender, SIMCONNECT_RECV_ENUMERATE_INPUT_EVENT_PARAMS data)
        {

            _logger.LogDebug($"Get the Event OnRecvEventEnumParams:");
            _logger.LogDebug($"  InputEvent ID: {data.Hash}");
            _logger.LogDebug($"  InputEvent Value: {data.Value}");

        }

        private void OnRecvGetInputEvent(SimConnect sender, SIMCONNECT_RECV_GET_INPUT_EVENT data)
        {
            switch (data.eType)
            {
                case SIMCONNECT_INPUT_EVENT_TYPE.DOUBLE:
                    double d = (double)data.Value[0];
                    _logger.LogDebug("Receive Double: " + d.ToString());
                    UpdateInputEventResult("获取InputEvents值："+d.ToString());
                    break;
                case SIMCONNECT_INPUT_EVENT_TYPE.STRING:
                    SimConnect.InputEventString str = (SimConnect.InputEventString)data.Value[0];
                    _logger.LogDebug("Receive String: " + str.value.ToString());
                    UpdateInputEventResult("获取InputEvents值:"+str.value.ToString());
                    break;
            }
            isGettingInputEvent = false;
        }
        public async Task<bool> EnumerateInputEvents()
        {
            if (!simConnectConnected)
            {
                MessageBox.Show("SimConnect未连接", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false ;
            }
            
            try
            {
                _logger.LogDebug("Starting input event enumeration...");
                var loop = 0;

                while (!isInputEventsLoaded)
                {
                    loop++;
                    simConnect.EnumerateInputEvents(DATA_REQUESTS.REQUEST_EnumerateInputEvents);
                    simConnect.ReceiveMessage();
                    await Task.Delay(5000);
                    _logger.LogDebug($"循环第{loop}次以获取InputEvents");
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug($"Enumeration failed: {ex.Message}");
            }
            
            return isInputEventsLoaded;
        }
        private void inputEventGetButton_Click(object sender, EventArgs e)
        {
            if (!simConnectConnected)
            {
                MessageBox.Show("SimConnect未连接", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            string selectedEvent = inputEventComboBox.SelectedItem?.ToString();
            string value = inputEventValueTextBox.Text.Trim();

            try
            {
                if (simConnectInputEvents.Keys.Contains(selectedEvent) && !isGettingInputEvent)
                {
                    ulong hashid;
                    hashid = (ulong)simConnectInputEvents.GetValueOrDefault(selectedEvent);
                    isGettingInputEvent = true;
                    simConnect.GetInputEvent(DATA_REQUESTS.RequestInputEvents, hashid);
                    simConnect.ReceiveMessage();
                   
                }
                else if (isGettingInputEvent)
                {
                    simConnect.ReceiveMessage();
                }
                else
                {
                    _logger.LogDebug($"{DateTime.Now.ToString()}没有找到:{selectedEvent}");
                }
            }
            catch (Exception ex)
            {
                UpdateInputEventResult($"获取失败: {ex.Message}");
            }
        }

        private void inputEventTriggerButton_Click(object sender, EventArgs e)
        {
            if (!simConnectConnected)
            {
                MessageBox.Show("SimConnect未连接", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string selectedEvent = inputEventComboBox.SelectedItem?.ToString();
            string value = inputEventValueTextBox.Text.Trim();

            if (string.IsNullOrEmpty(selectedEvent))
            {
                MessageBox.Show("请选择InputEvent", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                if (simConnectInputEvents.Keys.Contains(selectedEvent) && !isGettingInputEvent)
                {
                    ulong hashid;
                    InputEventFloatData fdata = new();
                    fdata.value_f64 = double.Parse(value);
                    hashid = (ulong)simConnectInputEvents.GetValueOrDefault(selectedEvent);
                    simConnect.SetInputEvent(hashid, fdata);
                    simConnect.ReceiveMessage();


                }
                else if (isGettingInputEvent)
                {
                    simConnect.ReceiveMessage();
                }
                else
                {
                    _logger.LogDebug($"{DateTime.Now.ToString()}没有找到:{selectedEvent}");
                }

                UpdateInputEventResult($"InputEvent触发成功: {selectedEvent}");
            }
            catch (Exception ex)
            {
                UpdateInputEventResult($"触发失败: {ex.Message}");
            }
        }

        private void inputEventComboBox_KeyUp(object sender, KeyEventArgs e)
        {
            // 实现搜索功能
            if (simConnectInputEvents.Keys.Count > 0)
            {
                string searchText = inputEventComboBox.Text;

                // 使用原始的事件名称列表进行过滤
                var filteredItems = simConnectInputEvents.Keys
                    .Where(item => item.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToArray();

                // 更新下拉列表的项，但不改变当前输入的文本
                inputEventComboBox.Items.Clear();
                inputEventComboBox.Items.AddRange(filteredItems);

                // 显示下拉列表（如果有匹配项）
                inputEventComboBox.DroppedDown = filteredItems.Length > 0 && !string.IsNullOrEmpty(searchText);

                // 确保光标保持在当前位置
                inputEventComboBox.SelectionStart = searchText.Length;
            }
        }
        #endregion


        #region LVAR功能
        private void lvarGetButton_Click(object sender, EventArgs e)
        {
            if (!simConnectConnected)
            {
                MessageBox.Show("SimConnect未连接", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string lvarName = lvarNameTextBox.Text.Trim();

            // 如果文本框为空，尝试从下拉列表获取
            if (string.IsNullOrEmpty(lvarName))
            {
                ComboBox lvarComboBox = lvarGroupBox.Controls.Find("lvarComboBox", true).FirstOrDefault() as ComboBox;
                if (lvarComboBox != null && lvarComboBox.SelectedItem != null)
                {
                    lvarName = lvarComboBox.SelectedItem.ToString();
                    lvarNameTextBox.Text = lvarName; // 同步到文本框
                }
            }

            if (string.IsNullOrEmpty(lvarName))
            {
                MessageBox.Show("LVAR名称不能为空", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // 发送LVAR名称到ClientData
                LVARRequestData requestData = new LVARRequestData { lvarName = lvarName };
                simConnect.SetClientData(ClientDataID.LVAR_REQUEST, DEFINITIONS.LVAR_REQUEST_DEFINITION,
                    SIMCONNECT_CLIENT_DATA_SET_FLAG.DEFAULT, 0, requestData);

                //simConnect.ReceiveMessage();
                //await Task.Delay(1000);

                // 触发读取事件
                //simConnect.TransmitClientEvent(0, LVAR_EVENTS.EVENT_LVAR_READ, 0, SIMCONNECT_GROUP_PRIORITY.HIGHEST, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);

                //await Task.Delay(1000);
                // 订阅响应ClientData的变化
                simConnect.RequestClientData(ClientDataID.LVAR_RESPONSE, DATA_REQUESTS.RESPONSE_LVAR_VALUE,
                    DEFINITIONS.LVAR_RESPONSE_DEFINITION, SIMCONNECT_CLIENT_DATA_PERIOD.ON_SET,
                    SIMCONNECT_CLIENT_DATA_REQUEST_FLAG.DEFAULT, 0, 0, 0);

                lvarResultLabel.Text = "正在获取LVAR值...";
            }
            catch (Exception ex)
            {
                lvarResultLabel.Text = $"获取失败: {ex.Message}";
            }
        }
        // 刷新列表按钮事件
        private void RefreshLvarListButton_Click(object sender, EventArgs e)
        {
            RequestLVARList();
        }

        #endregion
        protected override void WndProc(ref Message m)
        {
            // 处理SimConnect消息
            if (simConnectConnected && m.Msg == 0x402) // WM_USER_SIMCONNECT
            {
                simConnect?.ReceiveMessage();
            }
            base.WndProc(ref m);
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            simConnect?.Dispose();
        }
    }
}