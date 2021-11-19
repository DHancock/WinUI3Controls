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

```c#
<w3c:GroupBox Heading="Example Group Box">
    <Grid>
      <!-- content -->
    </Grid>
</w3c:GroupBox>
```

More worked examples can be found in the [TestSolution](TestSolution)

#### WinUI and Nuget Package Versions

In pre-release form, **WinUI** is contained in the **ProjectReunion** nuget package. Release versions are contained in the **WindowsAppSDK** nuget package. These two aren't compatible therefore:

If you are targeting **ProjectReunion** version 0.8.5 or above, use a WinUIControls library version < 2.0.0

If you are targeting **WindowsAppSDK** version 1.0.0 or above, use a WinUIControls library version >= 2.0.0

#### Release Notes

2.0.0   Change from ProjectReunion to WindowsAppSDK.
        Added theme resources for light, dark and high contrast themes.
        The default corner radius changes from 4 to 8.
        Minor code improvements.

1.0.1    Added the GitHub project URL. No code changes.

1.0.0    Initial release.
