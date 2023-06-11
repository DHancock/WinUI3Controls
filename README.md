### WinUI3Controls

A control library for WinUI 3

It contains a two controls, an WinUI implementation of the **WPF GroupBox** control and a simple palette based color picker.
The library is available as a [NuGet package](https://www.nuget.org/packages/AssyntSoftware.WinUI3Controls/).


![xwz2](https://github.com/DHancock/WinUI3Controls/assets/28826959/7122c0d9-9776-436a-bc77-f0b41ae679c4)

<details>

<summary>Source...</summary>

```xaml

<Page
    x:Class="TestSolution.ExamplePage"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
    xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
    xmlns:w3c="using:AssyntSoftware.WinUI3Controls"
    mc:Ignorable="d"
    Background="{ThemeResource ApplicationPageBackgroundThemeBrush}">

    <Grid Grid.ColumnDefinitions="200" RowDefinitions="Auto,Auto" RowSpacing="40" Margin="60">

        <w3c:GroupBox Heading="Options" CornerRadius="6" HeadingMargin="12">
            <Grid ColumnDefinitions="*,*,*" >
                <Button Grid.Column="0" Content="A" Margin="10"/>
                <Button Grid.Column="1" Content="B" Margin="10"/>
                <Button Grid.Column="2" Content="C" Margin="10"/>
            </Grid>
        </w3c:GroupBox>

        <w3c:SimpleColorPicker Grid.Row="1" IsMiniPalette="True" Color="{x:Bind ChosenColor, Mode=TwoWay}"/>

    </Grid>
</Page>

```

</details>


Further examples can be found in the [TestSolution](TestSolution)

### Release Notes

|Version|Changes|
|-------|:------|
|2.1.0|Add simple color picker control.|
|2.0.3|Draw border when the group box is the target of a TeachingTip. [#21](https://github.com/DHancock/WinUI3Controls/pull/21).|
|2.0.2|Fix adding size changed event handlers [#18](https://github.com/DHancock/WinUI3Controls/pull/18).|
|2.0.1|Fix the high contrast theme brush.| 
|2.0.0|Change from ProjectReunion to WindowsAppSDK.<br>Added theme resources for light, dark and high contrast themes.|
|1.0.1|Added the GitHub project URL. No code changes.|
|1.0.0|Initial release.|
