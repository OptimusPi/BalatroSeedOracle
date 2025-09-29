# FiltersModal Refactor Plan

## Current Mess
- 3 different implementations (FiltersModal, FiltersModalMVVM, ModularFiltersModal)
- None fully working
- Mixed code-behind and partial MVVM
- Broken bindings everywhere

## New Architecture: FiltersModalV2

### Core Components

#### 1. FilterClause Model
```csharp
public class FilterClause : ObservableObject
{
    public FilterItemType ItemType { get; set; }
    public string ItemValue { get; set; }
    public int[] Antes { get; set; }
    public FilterSource Sources { get; set; }
    public int Score { get; set; }
    public FilterCategory Category { get; set; } // Must/Should/MustNot
}
```

#### 2. FiltersViewModel
```csharp
public class FiltersViewModel : ViewModelBase
{
    private readonly IEventAggregator _events;
    private readonly IFilterService _filterService;
    
    public ObservableCollection<FilterClause> MustClauses { get; }
    public ObservableCollection<FilterClause> ShouldClauses { get; }
    public ObservableCollection<FilterClause> MustNotClauses { get; }
    
    public ICommand AddClauseCommand { get; }
    public ICommand RemoveClauseCommand { get; }
    public ICommand SaveFilterCommand { get; }
    public ICommand LoadFilterCommand { get; }
    public ICommand TestFilterCommand { get; }
    
    // Current clause being edited
    private FilterClause _currentClause;
    public FilterClause CurrentClause
    {
        get => _currentClause;
        set => SetProperty(ref _currentClause, value);
    }
}
```

#### 3. Reusable Controls

##### FilterClauseEditor.xaml
```xml
<UserControl x:Class="FilterClauseEditor">
    <Grid>
        <!-- Item Type Selector -->
        <ComboBox ItemsSource="{Binding ItemTypes}"
                  SelectedItem="{Binding ItemType}" />
        
        <!-- Dynamic Value Selector based on ItemType -->
        <ContentPresenter Content="{Binding}">
            <ContentPresenter.Resources>
                <DataTemplate x:Key="JokerTemplate">
                    <controls:JokerSelector />
                </DataTemplate>
                <DataTemplate x:Key="VoucherTemplate">
                    <controls:VoucherSelector />
                </DataTemplate>
            </ContentPresenter.Resources>
        </ContentPresenter>
        
        <!-- Ante Selector -->
        <controls:AnteRangeSelector Antes="{Binding Antes}" />
        
        <!-- Source Selector -->
        <controls:SourceSelector Sources="{Binding Sources}" />
    </Grid>
</UserControl>
```

##### AnteRangeSelector.xaml
```xml
<UserControl x:Class="AnteRangeSelector">
    <!-- Visual ante selector with checkboxes or range slider -->
    <ItemsControl ItemsSource="{Binding AnteOptions}">
        <ItemsControl.ItemTemplate>
            <DataTemplate>
                <CheckBox IsChecked="{Binding IsSelected}"
                          Content="{Binding AnteNumber}" />
            </DataTemplate>
        </ItemsControl.ItemTemplate>
    </ItemsControl>
</UserControl>
```

### 4. Tabs Structure

```xml
<TabControl>
    <TabItem Header="Visual Builder">
        <controls:VisualFilterBuilder />
    </TabItem>
    
    <TabItem Header="JSON Editor">
        <controls:JsonFilterEditor />
    </TabItem>
    
    <TabItem Header="Quick Presets">
        <controls:FilterPresets />
    </TabItem>
    
    <TabItem Header="Import/Export">
        <controls:FilterImportExport />
    </TabItem>
</TabControl>
```

## DuckDB Integration Changes

### Current Problem
- DB persists even after filter edit
- Stale data causes confusion
- Clone feature is broken

### Solution
```csharp
public class FilterService
{
    public async Task<FilterEditResult> EditFilter(string filterId)
    {
        // 1. Save current seeds if any
        var currentSeeds = await _dbService.GetFoundSeeds(filterId);
        
        // 2. Show edit dialog
        var editedFilter = await ShowFilterEditor(filterId);
        
        if (editedFilter.HasChanges)
        {
            // 3. Completely destroy old DB
            await _dbService.DeleteDatabase(filterId);
            
            // 4. Save edited filter
            await SaveFilter(editedFilter);
            
            // 5. Offer to re-search saved seeds
            if (currentSeeds.Any())
            {
                var result = await ShowDialog(
                    $"Found {currentSeeds.Count} seeds with old filter. " +
                    "Re-search with new filter?");
                
                if (result == DialogResult.Yes)
                {
                    await _searchService.SearchSpecificSeeds(
                        currentSeeds, 
                        editedFilter);
                }
            }
        }
    }
    
    // Remove confusing clone feature
    [Obsolete("Use Save As instead")]
    public Task CloneFilter(string filterId) => throw new NotSupportedException();
}
```

## Migration Strategy

### Week 1: Build Components
- [ ] Create FilterClause model
- [ ] Build AnteRangeSelector control
- [ ] Build ItemTypeSelector control
- [ ] Build SourceSelector control

### Week 2: Build FiltersModalV2
- [ ] Create FiltersViewModel
- [ ] Wire up bindings
- [ ] Test all operations
- [ ] Add to feature flags

### Week 3: Migration
- [ ] Add "Use New Filters UI" setting
- [ ] Run both in parallel for testing
- [ ] Gather feedback
- [ ] Fix issues

### Week 4: Cleanup
- [ ] Remove old FiltersModal
- [ ] Remove FiltersModalMVVM
- [ ] Remove ModularFiltersModal
- [ ] Update documentation

## Breaking Changes (Acceptable)

### Will Break:
1. Clone feature (removing it)
2. Saved filter format (will migrate)
3. Some keyboard shortcuts (will document)

### Will Preserve:
1. All filter functionality
2. Import/export capability
3. JSON editing option
4. Visual builder

## Testing Plan

```csharp
[TestFixture]
public class FiltersViewModelTests
{
    [Test]
    public void AddClause_AddsToCorrectCollection()
    {
        var vm = new FiltersViewModel();
        vm.CurrentClause = new FilterClause { Category = Must };
        
        vm.AddClauseCommand.Execute(null);
        
        Assert.AreEqual(1, vm.MustClauses.Count);
    }
    
    [Test]
    public void EditFilter_ResetsDatabase()
    {
        // Verify DB is destroyed on edit
    }
}
```

## Success Criteria

- [ ] One working FiltersModal instead of 3 broken ones
- [ ] All bindings work properly
- [ ] DB resets on filter edit
- [ ] Seeds can be re-searched after edit
- [ ] No more clone confusion
- [ ] Testable ViewModels
- [ ] Reusable components