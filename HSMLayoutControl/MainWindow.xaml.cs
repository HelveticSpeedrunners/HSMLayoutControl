using HSMLayoutManager.Controls;
using HSMLayoutManager.Views;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Xml;
using LiveSplitRun = HSMLayoutManager.LiveSplit.LiveSplitCore.Run;
using LiveSplitTimer = HSMLayoutManager.LiveSplit.LiveSplitCore.Timer;
using SystemTimer = System.Timers.Timer;

namespace HSMLayoutManager
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    private readonly LiveSplitTimer _livesplitTimer;
    private readonly SystemTimer _updateTimer = new SystemTimer();
    private readonly LiveSplitRun _livesplitRun = new LiveSplitRun();
    private readonly List<RunEditor> _editorList = new List<RunEditor>();

    private RunEditor _prevRun;
    private RunEditor _currentRun;
    private RunEditor _nextRun;

    private readonly LayoutHD _layoutHD = new LayoutHD();
    private readonly LayoutSD _layoutSD = new LayoutSD();
    private readonly LayoutIntermission _layoutIntermission = new LayoutIntermission();

    private TimerState _timerState;

    public MainWindow()
    {
      InitializeComponent();

      _livesplitRun.PushSegment(new LiveSplit.LiveSplitCore.Segment("seg"));
      _livesplitTimer = LiveSplitTimer.New(_livesplitRun);
      _livesplitTimer.Start();
      _livesplitTimer.InitializeGameTime();
      _livesplitTimer.PauseGameTime();
      _livesplitTimer.SetGameTime(LiveSplit.LiveSplitCore.TimeSpan.Parse("00:00:00"));

      ImportState(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "state.xml"));

      _updateTimer.Interval = 100;
      _updateTimer.Elapsed += UpdateAll;
      _updateTimer.Start();
    }

    private void UpdateAll(object sender, System.Timers.ElapsedEventArgs e)
    {
      App.Current.Dispatcher.Invoke(delegate
      {
        DateTime currentTime = DateTime.Now;
        _layoutHD.SetLocalTime(currentTime.Hour, currentTime.Minute);
        _layoutSD.SetLocalTime(currentTime.Hour, currentTime.Minute);
        _layoutIntermission.SetLocalTime(currentTime.Hour, currentTime.Minute);
        UpdateRunTimers();
      });
    }

    #region LiveSplit Timer

    private void StartTimer()
    {
      btnNextRun.IsEnabled = false;
      btnPrevRun.IsEnabled = false;
      txtSetTime.IsEnabled = false;

      if (_timerState != TimerState.Running)
      {
        _livesplitTimer.ResumeGameTime();
        _timerState = TimerState.Running;
        UpdateRunTimerHeader();
      }
    }

    private void PauseTimer()
    {
      txtSetTime.IsEnabled = true;
      txtSetTime.Text = lblTimer.Content as string;

      if (_timerState == TimerState.Running)
      {
        _livesplitTimer.PauseGameTime();
        _timerState = TimerState.Paused;
        UpdateRunTimerHeader();
      }
    }

    private void StopTimer()
    {
      txtSetTime.IsEnabled = true;

      _livesplitTimer.PauseGameTime();
      _timerState = TimerState.Stopped;
      UpdateRunTimerHeader();

      btnNextRun.IsEnabled = true;
      btnPrevRun.IsEnabled = true;

      _currentRun?.SuggestFinalTime(lblTimer.Content as string);
    }

    private void ResetTimer()
    {
      if ((_timerState == TimerState.Running || _timerState == TimerState.Paused)
        && MessageBox.Show("The timer has not been stopped yet, are you sure you want to reset?", "Warning!", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
      {
        return;
      }

      _livesplitTimer.SetGameTime(LiveSplit.LiveSplitCore.TimeSpan.Parse("00:00:00"));
      _timerState = TimerState.Reset;
      UpdateRunTimerHeader();

      btnNextRun.IsEnabled = true;
      btnPrevRun.IsEnabled = true;
      txtSetTime.IsEnabled = true;
    }

    private void SetTime()
    {
      string timeString = txtSetTime.Text.Trim();

      if (Regex.Match(timeString, "^[0-1][0-9]:[0-5][0-9]:[0-5][0-9]$").Success)
      {
        _livesplitTimer.SetGameTime(LiveSplit.LiveSplitCore.TimeSpan.Parse(timeString));
      }
      else
      {
        MessageBox.Show("Invalid Time! Note that the time must be provided in the format XX:XX:XX", "Error", MessageBoxButton.OK);
        txtSetTime.Text = "00:00:00";
      }
    }

    private void SetRunTime(int hours, int minutes, int seconds)
    {
      lblTimer.Content = String.Format("{0:D2}:{1:D2}:{2:D2}", hours, minutes, seconds);
    }

    private void UpdateRunTimers()
    {
      TimeSpan runTime = TimeSpan.FromSeconds(_livesplitTimer.CurrentTime().GameTime().TotalSeconds());
      _layoutHD.SetRunTime(runTime.Hours, runTime.Minutes, runTime.Seconds);
      _layoutSD.SetRunTime(runTime.Hours, runTime.Minutes, runTime.Seconds);
      SetRunTime(runTime.Hours, runTime.Minutes, runTime.Seconds);
    }

    private void UpdateRunTimerHeader()
    {
      gpxRunTimer.Header = String.Format("Run Timer ({0})", _timerState.ToString());
    }

    private void BtnStartTimer_Click(object sender, RoutedEventArgs e) => StartTimer();
    private void BtnResetTimer_Click(object sender, RoutedEventArgs e) => ResetTimer();
    private void BtnPauseTimer_Click(object sender, RoutedEventArgs e) => PauseTimer();
    private void BtnStopTimer_Click(object sender, RoutedEventArgs e) => StopTimer();
    private void BtnSetTime_Click(object sender, RoutedEventArgs e) => SetTime();

    #endregion LiveSplit Timer

    #region Run List Modifiers

    private void AddRun()
    {
      RunEditor r = new RunEditor();
      r.OnRemove += OnRemoveRun;
      r.OnMoveDown += OnMoveRunDown;

      _editorList.Add(r);
      stkEditors.Children.Add(_editorList.Last());
      if (_nextRun == null) _nextRun = r;
    }

    private void OnRemoveRun(object sender, EventArgs e)
    {
      if (sender is RunEditor r)
      {
        if (_nextRun == r)
        {
          if (_editorList.IndexOf(_nextRun) != _editorList.Count - 1) _nextRun = _editorList[_editorList.IndexOf(_nextRun) + 1];
          else _nextRun = null;
        }

        if (_editorList.Contains(r)) _editorList.Remove(r);
        if (stkEditors.Children.Contains(r)) stkEditors.Children.Remove(r);
      }
    }

    private void OnMoveRunDown(object sender, EventArgs e)
    {
      if (sender is RunEditor r)
      {
        int ind = _editorList.IndexOf(r);

        if (ind >= 0 && ind < stkEditors.Children.Count - 1)
        {
          _editorList.Remove(r);
          stkEditors.Children.Remove(r);
          _editorList.Insert(ind + 1, r);
          stkEditors.Children.Insert(ind + 1, r);

          if (r == _nextRun) _nextRun = _editorList[ind];
        }
      }
    }

    private void ActivateNextRun()
    {
      if (_currentRun == null && _nextRun != null)
      {
        _currentRun = _nextRun;
        if (_editorList.IndexOf(_currentRun) < _editorList.Count - 1) _nextRun = _editorList[_editorList.IndexOf(_currentRun) + 1];
        else _nextRun = null;
        _currentRun.MarkAsCurrentRun();
      }
      else if (_currentRun != null && _nextRun != null)
      {
        _currentRun.MarkAsDone();
        _prevRun = _currentRun;
        _currentRun = _nextRun;
        if (_editorList.IndexOf(_currentRun) < _editorList.Count - 1) _nextRun = _editorList[_editorList.IndexOf(_currentRun) + 1];
        else _nextRun = null;
        _currentRun.MarkAsCurrentRun();
      }
      else if (_currentRun != null)
      {
        _currentRun.MarkAsDone();
        _prevRun = _currentRun;
        _currentRun = null;
      }
    }

    private void ReturnToPreviousRun()
    {
      if (_prevRun != null)
      {
        _currentRun?.MarkAsOpen();
        _nextRun = _currentRun;
        _currentRun = _prevRun;
        if (_editorList.IndexOf(_prevRun) > 0) _prevRun = _editorList[_editorList.IndexOf(_prevRun) - 1];
        else _prevRun = null;
        _currentRun.MarkAsCurrentRun();
      }
      else if (_currentRun != null)
      {
        _currentRun.MarkAsOpen();
        _nextRun = _currentRun;
        _currentRun = null;
      }
    }

    private void RemoveAllRuns()
    {
      StopTimer();
      ResetTimer();

      _editorList.Clear();
      stkEditors.Children.Clear();
      _prevRun = null;
      _currentRun = null;
      _nextRun = null;
    }

    private void BtnAddRun_Click(object sender, RoutedEventArgs e) => AddRun();
    private void BtnNextRun_Click(object sender, RoutedEventArgs e) => ActivateNextRun();
    private void BtnPrevRun_Click(object sender, RoutedEventArgs e) => ReturnToPreviousRun();

    #endregion Run List Modifiers

    #region Layout Updates

    private void UpdateIntermission()
    {
      if (_prevRun != null)
      {
        string prevGameName = String.IsNullOrWhiteSpace(_prevRun.GameNameShort) ? _prevRun.GameName : _prevRun.GameNameShort;
        _layoutIntermission.SetLastGame(prevGameName, _prevRun.GameCategory, _prevRun.ScheduleTime);
      }
      else
      {
        _layoutIntermission.SetLastGame("Marathon Opening", "Quick%", "09:00");
      }

      if (_currentRun != null)
        _layoutIntermission.SetCurrentGame(_currentRun.GameName, _currentRun.GameCategory, _currentRun.ScheduleTime);
      else
        _layoutIntermission.SetCurrentGame("No Game Set", "No Category Set", "00:00");

      if (_nextRun != null)
        _layoutIntermission.SetNextGame(_nextRun.GameName, _nextRun.GameCategory, _nextRun.ScheduleTime);
      else
        _layoutIntermission.SetNextGame("Stream End!", ":(", "12:00");
    }

    private void UpdateAll()
    {
      if (_prevRun != null)
      {
        string prevGameName = String.IsNullOrWhiteSpace(_prevRun.GameNameShort) ? _prevRun.GameName : _prevRun.GameNameShort;
        _layoutHD.SetLastGame(prevGameName, _prevRun.GameCategory, _prevRun.FinalTime);
        _layoutSD.SetLastGame(prevGameName, _prevRun.GameCategory, _prevRun.FinalTime);
        _layoutIntermission.SetLastGame(prevGameName, _prevRun.GameCategory, _prevRun.ScheduleTime);
      }
      else
      {
        _layoutHD.SetLastGame("Marathon Opening", "Quick%", "03:34");
        _layoutSD.SetLastGame("Marathon Opening", "Quick%", "03:34");
        _layoutIntermission.SetLastGame("Marathon Opening", "Quick%", "09:00");
      }

      if (_currentRun != null)
      {
        _layoutHD.SetCurrentGame(_currentRun.GameName, _currentRun.GameCategory, _currentRun.Estimate, _currentRun.Runners);
        _layoutSD.SetCurrentGame(_currentRun.GameName, _currentRun.GameCategory, _currentRun.Estimate, _currentRun.Runners);
        _layoutIntermission.SetCurrentGame(_currentRun.GameName, _currentRun.GameCategory, _currentRun.ScheduleTime);
      }
      else
      {
        _layoutHD.SetCurrentGame("No Game Set", "No Category Set", "No Estimate Set", "No Runners Set");
        _layoutSD.SetCurrentGame("No Game Set", "No Category Set", "No Estimate Set", "No Runners Set");
        _layoutIntermission.SetCurrentGame("No Game Set", "No Category Set", "00:00");
      }

      if (_nextRun != null)
      {
        _layoutHD.SetNextGame(_nextRun.GameName, _nextRun.GameCategory, _nextRun.ScheduleTime, _nextRun.Runners);
        _layoutSD.SetNextGame(_nextRun.GameName, _nextRun.GameCategory, _nextRun.ScheduleTime, _nextRun.Runners);
        _layoutIntermission.SetNextGame(_nextRun.GameName, _nextRun.GameCategory, _nextRun.ScheduleTime);
      }
      else
      {
        _layoutHD.SetNextGame("Stream End!", ":(", "12:00", "Everyone");
        _layoutSD.SetNextGame("Stream End!", ":(", "12:00", "Everyone");
        _layoutIntermission.SetNextGame("Stream End!", ":(", "12:00");
      }
    }

    private void BtnUpdateIntermission_Click(object sender, RoutedEventArgs e) => UpdateIntermission();
    private void BtnUpdateAll_Click(object sender, RoutedEventArgs e) => UpdateAll();

    #endregion Layout Updates

    #region IO

    private void BtnExport_Click(object sender, RoutedEventArgs e)
    {
      XmlDocument xmlDoc = new XmlDocument();
      xmlDoc.AppendChild(xmlDoc.CreateXmlDeclaration("1.0", "UTF-8", null));
      xmlDoc.AppendChild(xmlDoc.CreateElement("state"));

      XmlElement runs = xmlDoc.CreateElement("runs");
      foreach (RunEditor re in _editorList)
      {
        XmlElement r = xmlDoc.CreateElement("run");

        XmlElement index = xmlDoc.CreateElement("index");
        index.InnerText = _editorList.IndexOf(re).ToString();
        r.AppendChild(index);

        XmlElement game = xmlDoc.CreateElement("game");
        game.InnerText = re.GameName;
        r.AppendChild(game);

        XmlElement shortgame = xmlDoc.CreateElement("shortgame");
        shortgame.InnerText = re.GameNameShort;
        r.AppendChild(shortgame);

        XmlElement category = xmlDoc.CreateElement("category");
        category.InnerText = re.GameCategory;
        r.AppendChild(category);

        XmlElement estimate = xmlDoc.CreateElement("estimate");
        estimate.InnerText = re.Estimate;
        r.AppendChild(estimate);

        XmlElement finalTime = xmlDoc.CreateElement("time");
        finalTime.InnerText = re.FinalTime;
        r.AppendChild(finalTime);

        XmlElement runners = xmlDoc.CreateElement("runners");
        runners.InnerText = re.Runners;
        r.AppendChild(runners);

        XmlElement releaseYear = xmlDoc.CreateElement("year");
        releaseYear.InnerText = re.GameYear;
        r.AppendChild(releaseYear);

        XmlElement platform = xmlDoc.CreateElement("platform");
        platform.InnerText = re.Platform;
        r.AppendChild(platform);

        XmlElement scheduleTime = xmlDoc.CreateElement("schedule");
        scheduleTime.InnerText = re.ScheduleTime;
        r.AppendChild(scheduleTime);

        runs.AppendChild(r);
      }

      XmlElement currentRun = xmlDoc.CreateElement("current");
      if (_currentRun != null) currentRun.InnerText = _editorList.IndexOf(_currentRun).ToString();
      else currentRun.InnerText = "0";

      xmlDoc["state"].AppendChild(runs);
      xmlDoc["state"].AppendChild(currentRun);
      DateTime now = DateTime.Now;
      string timeString = String.Format("{0}{1:D2}{2:D2}{3:D2}{4:D2}{5:D2}", now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second);
      if (!System.IO.Directory.Exists(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Backlog")))
        System.IO.Directory.CreateDirectory(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Backlog"));
      xmlDoc.Save(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Backlog", timeString + ".state.xml"));
      xmlDoc.Save(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "state.xml"));

      MessageBox.Show("Successfully Exported!");
    }

    private void BtnImport_Click(object sender, RoutedEventArgs e)
    {
      if (_editorList.Count == 0 || MessageBox.Show("This will remove all runs. Continue?", "Warning!", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
      {
        OpenFileDialog ofd = new OpenFileDialog();

        if (ofd.ShowDialog() == true)
        {
          RemoveAllRuns();
          ImportState(ofd.FileName);
        }
      }
    }

    private void ImportState(string path)
    {
      if (System.IO.File.Exists(path))
      {
        XmlDocument xmlDoc = new XmlDocument();

        try
        {
          xmlDoc.Load(path);
        }
        catch
        {
          MessageBox.Show("Failed to load configuration file! (Exception)");
        }

        foreach (XmlNode n in xmlDoc["state"]["runs"])
        {
          AddRun();
          _editorList.Last().Measure(new Size(Width, Height));
          _editorList.Last().Arrange(new Rect(0, 0, _editorList.Last().DesiredSize.Width, _editorList.Last().DesiredSize.Height));
          _editorList.Last().Hide();
          _editorList.Last().ImportConfig(n);
        }

        for (int i = 0; i <= Int32.Parse(xmlDoc["state"]["current"].InnerText); i++) ActivateNextRun();

        _currentRun?.UnHide();
        UpdateAll();
      }
      else
      {
        MessageBox.Show(String.Format("{0} Not Found", path));
      }
    }

    #endregion IO

    #region Window Control

    private bool WarnBeforeClosing()
    {
      return MessageBox.Show("Are you 100% sure you want exit the application?", "WARNING!", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
      if (WarnBeforeClosing())
      {
        if (_updateTimer != null)
        {
          _updateTimer.Stop();
          _updateTimer.Elapsed -= UpdateAll;
          _updateTimer.Dispose();
        }

        _layoutHD.Close();
        _layoutSD.Close();
        _layoutIntermission.Close();

        _livesplitTimer.Reset(false);

        e.Cancel = false;
      }
      else
      {
        e.Cancel = true;
      }
    }

    private void BtnShowLayoutHD_Click(object sender, RoutedEventArgs e) => _layoutHD.Show();
    private void BtnShowLayoutSD_Click(object sender, RoutedEventArgs e) => _layoutSD.Show();
    private void BtnShowLayoutIntermission_Click(object sender, RoutedEventArgs e) => _layoutIntermission.Show();

    #endregion Window Control

    private enum TimerState : uint
    {
      Reset = 0,
      Running = 1,
      Paused = 2,
      Stopped = 3
    }
  }
}
