TechFixStudio/
│
├── TechFixStudio.csproj
├── App.xaml
├── App.xaml.cs
├── MainWindow.xaml
├── MainWindow.xaml.cs          ← 替换
├── app.manifest
│
├── Infrastructure/
│   └── AppPaths.cs
│
├── Models/
│   └── Models.cs               ← 替换
│
├── ViewModels/
│   └── MainViewModel.cs        ← 替换
│
└── Services/
    ├── AndroidService.cs       ← 替换
    ├── AndroidDiagnosticsService.cs  ← 新增
    ├── RootMagiskService.cs    ← 新增
    ├── FactoryImageService.cs  ← 替换
    │
    ├── AuditService.cs
    ├── FastbootService.cs
    ├── FlashQueueService.cs
    ├── HistoryService.cs
    ├── JsonStore.cs
    ├── ModuleService.cs
    ├── ProcessRunner.cs
    ├── RiskEngine.cs
    ├── RomCatalogService.cs
    ├── Sha256Service.cs
    ├── SshService.cs
    ├── ToolLocator.cs
    └── WindowsRepairService.cs
    
    
    修改记录：
    TechFixStudio/
│
├── TechFixStudio.csproj
├── App.xaml
├── App.xaml.cs
├── MainWindow.xaml
├── MainWindow.xaml.cs
├── app.manifest
│
├── Infrastructure/
│   └── AppPaths.cs
│
├── Models/
│   └── Models.cs                 ← 替换
│
├── ViewModels/
│   └── MainViewModel.cs          ← 替换
│
└── Services/
    ├── AndroidService.cs         ← 替换
    ├── AndroidDiagnosticsService.cs  ← 新增
    ├── AuditService.cs
    ├── FactoryImageService.cs    ← 替换
    ├── FastbootParser.cs         ← 新增
    ├── FastbootService.cs        ← 替换
    ├── FlashPlanService.cs       ← 新增
    ├── FlashQueueService.cs      ← 替换
    ├── HistoryService.cs
    ├── JsonStore.cs              ← 替换
    ├── MagiskService.cs          ← 新增
    ├── ModuleService.cs
    ├── ProcessRunner.cs          ← 替换
    ├── RiskEngine.cs             ← 替换
    ├── RomCatalogService.cs
    ├── Sha256Service.cs          ← 替换
    ├── SshService.cs
    ├── ToolLocator.cs
    └── WindowsRepairService.cs
    
    
    