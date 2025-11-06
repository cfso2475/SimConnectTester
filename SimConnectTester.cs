using Microsoft.Extensions.Logging;
using Microsoft.FlightSimulator.SimConnect;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
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
        enum DEFINITIONS
        {
            SimVarDefinition,
            SimEventDefinition,
            SIMVAR_DOUBLE,
            SIMVAR_STRING
        }

        enum DATA_REQUESTS
        {
            RequestSimVar,
            RequestInputEvents
        }
        public enum SIMCONNECT_GROUP_PRIORITY : uint
        {
            HIGHEST = 1,
            HIGHEST_MASKABLE = 10000000,
            STANDARD = 1900000000,
            DEFAULT = 2000000000,
            LOWEST = 4000000000
        }

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

        private static Dictionary<string, object> simConnectInputEvents = new Dictionary<string, object>();
        private bool isInputEventsLoaded = false;
        private bool isGettingInputEvent = false;
        public struct InputEventFloatData
        {
            public double value_f64;
        };

        private System.ComponentModel.IContainer components = null;

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
                simConnect = new SimConnect("Flight Simulator Controller", this.Handle, 0x402, null, 0);

                // 注册SimConnect事件处理
                simConnect.OnRecvOpen += new SimConnect.RecvOpenEventHandler(SimConnect_OnRecvOpen);
                simConnect.OnRecvQuit += new SimConnect.RecvQuitEventHandler(SimConnect_OnRecvQuit);
                simConnect.OnRecvException += new SimConnect.RecvExceptionEventHandler(SimConnect_OnRecvException);
                simConnect.OnRecvSimobjectData += new SimConnect.RecvSimobjectDataEventHandler(SimConnect_OnRecvSimobjectData);
                simConnect.OnRecvEnumerateInputEvents += OnRecvEventEnum;
                simConnect.OnRecvEnumerateInputEventParams += OnRecvEventEnumParams;
                simConnect.OnRecvGetInputEvent += OnRecvGetInputEvent;

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
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(600, 700);
            this.Text = "Flight Simulator Controller";
            this.StartPosition = FormStartPosition.CenterScreen;

            InitializeSimVarSection();
            InitializeSimEventSection();
            InitializeInputEventSection();
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
            inputEventComboBox.Size = new Size(300, 21);
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
            inputEventGetButton.Text = "获取事件列表";
            inputEventGetButton.Location = new Point(420, 30);
            inputEventGetButton.Size = new Size(120, 30);
            inputEventGetButton.Click += inputEventGetButton_Click;
            inputEventGroupBox.Controls.Add(inputEventGetButton);

            inputEventTriggerButton = new Button();
            inputEventTriggerButton.Text = "触发";
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
        }
        #region SimConnect事件处理
        private void SimConnect_OnRecvOpen(SimConnect sender, SIMCONNECT_RECV_OPEN data)
        {
            UpdateStatus("已连接到Microsoft Flight Simulator");
        }

        private void SimConnect_OnRecvQuit(SimConnect sender, SIMCONNECT_RECV data)
        {
            UpdateStatus("Flight Simulator已断开连接");
            simConnectConnected = false;
        }

        private void SimConnect_OnRecvException(SimConnect sender, SIMCONNECT_RECV_EXCEPTION data)
        {
            UpdateStatus($"SimConnect异常: {data.dwException}");
        }

        private void SimConnect_OnRecvSimobjectData(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA data)
        {
            switch ((DATA_REQUESTS)data.dwRequestID)
            {
                case DATA_REQUESTS.RequestSimVar:
                    // 处理SimVar数据返回
                    double simVarValue = (double)data.dwData[0];
                    UpdateSimVarResult($"获取成功: {simVarValue}");
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
        #endregion

        #region SimVar功能
        private void simVarGetButton_Click(object sender, EventArgs e)
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
                    simConnect.AddToDataDefinition(DEFINITIONS.SIMVAR_DOUBLE, name, null, SIMCONNECT_DATATYPE.STRING256, 0, SimConnect.SIMCONNECT_UNUSED);
                    simConnect.RegisterDataDefineStruct<double>(DEFINITIONS.SIMVAR_DOUBLE);
                    // 请求SimVar数据
                    simConnect.RequestDataOnSimObject(DATA_REQUESTS.RequestSimVar, DEFINITIONS.SIMVAR_DOUBLE, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.ONCE, 0, 0, 0, 0);
                }
                else
                {
                    simConnect.AddToDataDefinition(DEFINITIONS.SIMVAR_STRING, name, type, SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                    simConnect.RegisterDataDefineStruct<double>(DEFINITIONS.SIMVAR_STRING);
                    // 请求SimVar数据
                    simConnect.RequestDataOnSimObject(DATA_REQUESTS.RequestSimVar, DEFINITIONS.SIMVAR_STRING, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.ONCE, 0, 0, 0, 0);
                }

                UpdateSimVarResult("正在获取SimVar...");
            }
            catch (Exception ex)
            {
                UpdateSimVarResult($"获取失败: {ex.Message}");
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
                        simConnect.AddToDataDefinition(DEFINITIONS.SIMVAR_DOUBLE, name, null, SIMCONNECT_DATATYPE.STRING256, 0, SimConnect.SIMCONNECT_UNUSED);
                        simConnect.RegisterDataDefineStruct<double>(DEFINITIONS.SIMVAR_DOUBLE);
                        // 请求SimVar数据
                        simConnect.RequestDataOnSimObject(DATA_REQUESTS.RequestSimVar, DEFINITIONS.SIMVAR_DOUBLE, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.ONCE, 0, 0, 0, 0);
                        // 设置SimVar值
                        simConnect.SetDataOnSimObject(DEFINITIONS.SIMVAR_DOUBLE, SimConnect.SIMCONNECT_OBJECT_ID_USER, 0, new double[] { doubleValue });
                    }
                    else
                    {
                        UpdateSimVarResult("Value必须是有效的数字");
                    }
                }
                else
                {
                    simConnect.AddToDataDefinition(DEFINITIONS.SIMVAR_STRING, name, type, SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                    simConnect.RegisterDataDefineStruct<double>(DEFINITIONS.SIMVAR_STRING);
                    // 请求SimVar数据
                    simConnect.RequestDataOnSimObject(DATA_REQUESTS.RequestSimVar, DEFINITIONS.SIMVAR_STRING, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.ONCE, 0, 0, 0, 0);
                    simConnect.SetDataOnSimObject(DEFINITIONS.SIMVAR_STRING, SimConnect.SIMCONNECT_OBJECT_ID_USER, 0, value );
                }
                    
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
                if (event_index > 9)
                    simConnect.ClearNotificationGroup(GROUP_ID.GROUP_1);

                simConnect.MapClientEventToSimEvent((FIXED_SIM_EVENTS)event_index, name);
                simConnect.AddClientEventToNotificationGroup(GROUP_ID.GROUP_1, (FIXED_SIM_EVENTS)event_index, false);
                event_index++;


                if (!string.IsNullOrEmpty(value) && double.TryParse(value, out double eventValue))
                {
                    simConnect.TransmitClientEvent(SimConnect.SIMCONNECT_OBJECT_ID_USER, (FIXED_SIM_EVENTS)event_index, (uint)eventValue, SIMCONNECT_GROUP_PRIORITY.HIGHEST, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                }
                else
                {
                    simConnect.TransmitClientEvent(SimConnect.SIMCONNECT_OBJECT_ID_USER, (FIXED_SIM_EVENTS)event_index, 0, SIMCONNECT_GROUP_PRIORITY.HIGHEST, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                }

                simEventResultLabel.Text = $"事件触发成功: {name}";
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

                    simConnectInputEvents.Add(msg.Name, msg.Hash);
                    inputEventComboBox.Items.Add(msg.Name);
                    simConnect.EnumerateInputEventParams(msg.Hash);
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
            if (inputEventComboBox.Items.Count > 0)
            {
                string searchText = inputEventComboBox.Text.ToLower();
                var filteredItems = inputEventsList.Where(item => item.ToLower().Contains(searchText)).ToArray();

                inputEventComboBox.Items.Clear();
                inputEventComboBox.Items.AddRange(filteredItems);

                if (filteredItems.Length > 0)
                {
                    inputEventComboBox.DroppedDown = true;
                }
            }
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