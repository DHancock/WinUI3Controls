### WinUI3Controls

A control library for WinUI 3

It currently contains single control which provides a WinUI implementation of the **WPF GroupBox** control.\
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
