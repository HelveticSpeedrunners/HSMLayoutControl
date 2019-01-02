using System;
using System.Windows;

namespace HSMLayoutManager.Views
{
  public partial class LayoutIntermission : Window
  {
    public LayoutIntermission()
    {
      InitializeComponent();
    }

    public void SetLocalTime(int hours, int minutes)
    {
      lblCurrentTime.Content = String.Format("{0:D2}:{1:D2}", hours, minutes);
    }

    public void SetCurrentGame(string name, string category, string scheduleTime)
    {
      lblGameRun1.Content = name;
      lblCategoryRun1.Content = category;
      lblScheduleRun1.Content = scheduleTime;
    }

    public void SetLastGame(string gameName, string category, string scheduleTime)
    {
      lblGameRun0.Content = gameName;
      lblCategoryRun0.Content = category;
      lblScheduleRun0.Content = scheduleTime;
    }

    public void SetNextGame(string gameName, string category, string scheduleTime)
    {
      lblGameRun2.Content = gameName;
      lblCategoryRun2.Content = category;
      lblScheduleRun2.Content = scheduleTime;
    }

    #region Window Control

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
      e.Cancel = true;
      Hide();
    }

    new public void Close()
    {
      Closing -= Window_Closing;
      base.Close();
    }

    #endregion Window Control

  }
}
