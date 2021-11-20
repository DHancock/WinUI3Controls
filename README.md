### WinUI3Controls

A control library for WinUI 3

It currently contains a single control which provides a WinUI implementation of the **WPF GroupBox** control.\
The library can be downloaded from NuGet. Search for "AssyntSoftware.WinUI3Controls"\
To use the control:

1) Add a xmlns identifier for the package to your container

```c#
xmlns:w3c="using:AssyntSoftware.WinUI3Controls"
```

2) Wrap your content in the GroupBox

```xml
<w3c:GroupBox Heading="Example Group Box">
    <Grid>
      <!-- content -->
    </Grid>
</w3c:GroupBox>
```

More worked examples can be found in the [TestSolution](TestSolution)

### WinUI and Nuget Package Versions

In pre-release form, **WinUI** is contained in the **ProjectReunion** nuget package. Release versions are contained in the **WindowsAppSDK** nuget package. These two aren't compatible therefore:

If you are targeting **ProjectReunion** version 0.8.5 or above, use a WinUIControls library version < 2.0.0

If you are targeting **WindowsAppSDK** version 1.0.0 or above, use a WinUIControls library version >= 2.0.0

### Release Notes

<table>
<thead>
<tr>
<th>Version</th>
<th align="left">Changes</th>
</tr>
</thead>
<tbody>
<tr>
<td>2.0.0</td>
<td>Change from ProjectReunion to WindowsAppSDK.<br>Added theme resources for light, dark and high contrast themes.<br>The default corner radius changes from 4 to 8.*<br>Minor code improvements.</td>
</tr>
<tr>
<td>1.0.1</td>
<td>Added the GitHub project URL. No code changes.</td>
</tr>
<tr>
<td>1.0.0</td>
<td>Initial release.</td>
</tr>
</tbody>
</table>


\* The GroupBox corner radius was defined by the system resource "OverlayCornerRadius" which increased to 8 with WindowsAppSDK 1.0.0. I decided to keep the new radius because I think it should increase to accommodate the rounded corners of WinUI controls. As with all UI designs, that is a subjective call. If you prefer the default WPF look, add this implicit style to your App.xaml file:

```xml
<Style TargetType="w3c:GroupBox">
    <Setter Property="CornerRadius" Value="4"/>
    <Setter Property="HeadingMargin" Value="12"/>
</Style>
```
