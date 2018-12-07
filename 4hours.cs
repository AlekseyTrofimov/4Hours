//||========================================================================||
//||	4Hours																||
//||																		||
//||	Данная программа следит за временем активной сессии и 				||
//||	предупреждает о необходимости сделать перерыв						||
//||																		||
//||  Компилировать с помощью команды										||
//||	csc.exe /target:winexe 4hours.cs /win32icon:ico_4hours.ico			||
//||																		||
//||	Aleksey Trofimov, 2018												||
//||																		||
//||========================================================================||

using System;
using System.IO;
using System.Windows.Forms;//MessageBox,Timer
using System.Drawing;
using Microsoft.Win32;

class T4Hours
{
	private static string logpath = AppDomain.CurrentDomain.BaseDirectory+"log";
	
	private static void LogWrite(string s) 
	{
		Directory.CreateDirectory(logpath);
		File.AppendAllText("log/log_" +  DateTime.Now.ToString(@"yyyy_MM_dd") + ".txt", DateTime.Now.ToString() + " " + s + Environment.NewLine);
	}	
	// ---------------------
	private static System.Windows.Forms.Timer myTimer = new System.Windows.Forms.Timer();
	private static int alarmCounter = 0; // Количество отображенных сообщений
	private static NotifyIcon notifyIcon = new NotifyIcon();
	private static ContextMenu contextMenu = new ContextMenu();
	
	private const int cT_B = 30; // Время до сообщения 30 Min;
	private const int cT_I = 30; // Интервал таймера (секунды)
	private static bool F_M;	 // Флаг, что нужно показать сперва уведомление
	
	private static int C_LogIn = 0;		// Количество входов в систему
	private static int C_LogOff = 0;	// Количество выходов из системы
	
	private static DateTime TimeStartAct;	// Начало активности, запущена 4Hours или разблокирован ПК
	private static DateTime TimeStopAct;	// Когда показывать уведомление
	private static DateTime TimeLogOff 		= DateTime.Now;		//Когда был заблокирован
	private static TimeSpan AllTimeLogOff	= TimeSpan.Zero;	//Время блокировки ПК
	private static TimeSpan AllTimeLogIn	= TimeSpan.Zero;	//Время активности
	
	// Запуск таймера
	private static void Start()
	{
		F_M = true;
		myTimer.Enabled = true;	
		TimeStartAct = DateTime.Now;
		TimeStopAct = TimeStartAct.AddMinutes(cT_B);
		LogWrite("Set timer " + TimeStopAct.ToString("HH:mm:ss") );
	}
	
	// Проверка по таймеру, если наступило время вывода сообщения
	private static void Update(Object myObject, EventArgs myEventArgs) 
	{
		if (DateTime.Now > TimeStopAct)
		{
			if (F_M) // Показываем уведомление в трее:
			{
				F_M = false;
				notifyIcon.BalloonTipText = "== Get up! ==";
				notifyIcon.ShowBalloonTip(5*1000);
				LogWrite("show balloon 'Get up!'");
			}
			else // Показываем сообщние:
			{
				alarmCounter+=1;
				myTimer.Stop();
				LogWrite("show message 'Get up!!'");
				MessageBox.Show("Get up!!", "4hours : "+alarmCounter, MessageBoxButtons.OK, MessageBoxIcon.Information);
				myTimer.Enabled = true;
				F_M = true;
				TimeStopAct = DateTime.Now.AddSeconds(45);
			}			
		}
	}

	// Отображение уведомления о состоянии 
	private static void TrayIcon_Click(object sender, EventArgs e)
	{
		notifyIcon.BalloonTipText =
			TimeStopAct.Subtract(DateTime.Now).ToString(@"hh\:mm\:ss")
		+ " to left\n"
		+ (AllTimeLogIn+(DateTime.Now-TimeStartAct)).ToString(@"hh\:mm\:ss") 
		+ " (" + C_LogIn + ") activ \n" 
		+ AllTimeLogOff.ToString(@"hh\:mm\:ss")
		+ " (" + C_LogOff + ") sleep " ;
		notifyIcon.ShowBalloonTip(3*1000);
	}
	// Перехват логофа/логина 
	private static void SystemEvents_SessionSwitch(object sender, Microsoft.Win32.SessionSwitchEventArgs e)
	{
		if (e.Reason == SessionSwitchReason.SessionLock)
		{ 
			 myTimer.Stop();
			 C_LogOff++;
			 TimeLogOff = DateTime.Now;
			 AllTimeLogIn += (TimeLogOff - TimeStartAct);
			 
			 LogWrite("I left! Activity: " 
			 + (TimeLogOff - TimeStartAct).ToString(@"hh\:mm\:ss")
			 + " - all: "
			 + AllTimeLogIn.ToString(@"hh\:mm\:ss"));
		}
		else if (e.Reason == SessionSwitchReason.SessionUnlock)
		{ 
			C_LogIn++;
			AllTimeLogOff += (DateTime.Now - TimeLogOff);
			
			LogWrite("I returned! Stop: " 
			 + (DateTime.Now - TimeLogOff).ToString(@"hh\:mm\:ss")
			 + " - all: "			
			+ AllTimeLogOff.ToString(@"hh\:mm\:ss"));
			
			Start();
		}
	}
	
	private static void menuExit(object sender, System.EventArgs e)
	{	
		Application.Exit();
	}
	
	private static void Main()
	{
		
		// Отслеживание логин/логофф:
		Microsoft.Win32.SystemEvents.SessionSwitch += new Microsoft.Win32.SessionSwitchEventHandler(SystemEvents_SessionSwitch);
		// Иконка в трее:
		notifyIcon.BalloonTipTitle = "4Hours";
		notifyIcon.BalloonTipIcon = ToolTipIcon.Info;
		notifyIcon.Icon = new Icon(@"ico_4hours.ico");
		notifyIcon.Visible = true;
		notifyIcon.Click += TrayIcon_Click;
		// Меню (пкм на иконке) с командой выход
		MenuItem menuItemExit = new MenuItem{Text = "Exit"};
		menuItemExit.Click += new System.EventHandler(menuExit);
		contextMenu.MenuItems.Add(menuItemExit); 
		notifyIcon.ContextMenu = contextMenu; 
		
		myTimer.Tick += new EventHandler(Update);
		myTimer.Interval = cT_I*1000;
		myTimer.Start();
		
		Start();
		LogWrite("Start 4Hours");
		
		Application.Run();
	}
	
	

}