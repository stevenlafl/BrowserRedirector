using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using DefaultBrowser.ViewModels;
using System;
using System.Diagnostics;

namespace DefaultBrowser.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
        // Set the TopLevel property for file dialogs
        this.AttachedToVisualTree += Window_AttachedToVisualTree;
        this.DataContextChanged += Window_DataContextChanged;
        this.Opened += Window_Opened;
    }
    
    private void Window_AttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        Debug.WriteLine("Window attached to visual tree");
        UpdateTopLevelInViewModel();
    }
    
    private void Window_DataContextChanged(object? sender, EventArgs e)
    {
        Debug.WriteLine("Window data context changed");
        UpdateTopLevelInViewModel();
    }
    
    private void Window_Opened(object? sender, EventArgs e)
    {
        Debug.WriteLine("Window opened");
        UpdateTopLevelInViewModel();
    }
    
    private void UpdateTopLevelInViewModel()
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            Debug.WriteLine("Setting CurrentTopLevel in view model");
            viewModel.CurrentTopLevel = this;
        }
        else
        {
            Debug.WriteLine("DataContext is not MainWindowViewModel");
        }
    }
}