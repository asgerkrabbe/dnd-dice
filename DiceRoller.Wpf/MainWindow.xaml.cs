using System.Windows;
using DiceEngine.Parsing;
using DiceEngine.Random;
using DiceRoller.Wpf.ViewModels;

namespace DiceRoller.Wpf;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        var parser = new DiceParser();
        var roller = new DiceEngine.Rolling.DiceRoller(new RandomRandomSource());
        DataContext = new MainViewModel(parser, roller);
    }
}
