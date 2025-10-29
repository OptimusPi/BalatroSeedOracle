# CardFlipRevealBehavior Usage Guide

The `CardFlipRevealBehavior` has been updated to support converter-based Image.Source bindings. This allows it to work with images that use converters like `ItemNameToSpriteConverter`.

## Key Features

1. **Two Operation Modes:**
   - **Legacy Mode** (default): Uses the `RevealSprite` property directly
   - **WatchSource Mode**: Watches `Image.Source` property changes from converters

2. **Animation Sequence:**
   - Shows deck back sprite
   - Horizontal "pinch" animation (scaleX: 1 → 0)
   - Swaps to real sprite at scaleX=0
   - Unpinch animation (scaleX: 0 → 1)
   - "Juice up" bounce for polish

3. **Smart Caching:** When using WatchSource mode, the converted sprite is cached before showing the deck back

## Usage Examples

### Example 1: Converter-Based Binding (NEW)

```xml
<Image>
    <Image.Source>
        <Binding Path="." Converter="{StaticResource ItemNameToSpriteConverter}" />
    </Image.Source>
    <Interaction.Behaviors>
        <behaviors:CardFlipRevealBehavior
            WatchSource="True"
            DeckName="Red"
            Delay="0:0:0.2"
            AutoTrigger="True" />
    </Interaction.Behaviors>
</Image>
```

Or the short form:

```xml
<Image Source="{Binding ., Converter={StaticResource ItemNameToSpriteConverter}}"
       Width="36" Height="48"
       Stretch="Uniform">
    <Interaction.Behaviors>
        <behaviors:CardFlipRevealBehavior
            WatchSource="True"
            DeckName="Red" />
    </Interaction.Behaviors>
</Image>
```

### Example 2: Legacy Direct Binding

```xml
<Image Width="71" Height="95">
    <Interaction.Behaviors>
        <behaviors:CardFlipRevealBehavior
            RevealSprite="{Binding JokerSprite}"
            DeckName="Anaglyph"
            AutoTrigger="True" />
    </Interaction.Behaviors>
</Image>
```

### Example 3: Staggered Animations in a List

```xml
<ItemsControl ItemsSource="{Binding ShopItems}">
    <ItemsControl.ItemTemplate>
        <DataTemplate>
            <Image Source="{Binding ., Converter={StaticResource ShopItemSpriteConverter}}"
                   Width="71" Height="95">
                <Interaction.Behaviors>
                    <behaviors:CardFlipRevealBehavior
                        WatchSource="True"
                        DeckName="{Binding DeckType}"
                        Delay="{Binding AnimationDelay}" />
                </Interaction.Behaviors>
            </Image>
        </DataTemplate>
    </ItemsControl.ItemTemplate>
</ItemsControl>
```

### Example 4: Manual Trigger

```xml
<Image x:Name="MyCard"
       Source="{Binding ItemName, Converter={StaticResource ItemNameToSpriteConverter}}">
    <Interaction.Behaviors>
        <behaviors:CardFlipRevealBehavior
            x:Name="FlipBehavior"
            WatchSource="True"
            DeckName="Red"
            AutoTrigger="False" />
    </Interaction.Behaviors>
</Image>

<!-- Trigger from code-behind or ViewModel -->
<Button Content="Reveal Card"
        Command="{Binding TriggerFlipCommand}"
        CommandParameter="{Binding #FlipBehavior}" />
```

Code-behind:
```csharp
// In ViewModel or code-behind
public void OnRevealCard()
{
    FlipBehavior.TriggerFlip();
}
```

## Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `WatchSource` | `bool` | `false` | Enable to watch Image.Source changes (for converter bindings) |
| `RevealSprite` | `IImage?` | `null` | Direct sprite to reveal (legacy mode) |
| `DeckName` | `string` | `"Red"` | Deck type for the card back sprite |
| `Delay` | `TimeSpan` | `0:0:0` | Delay before animation starts |
| `AutoTrigger` | `bool` | `true` | Automatically trigger on attach/source change |

## Important Notes

1. **WatchSource Mode:**
   - Set `WatchSource="True"` when using converter-based bindings
   - The behavior will cache the converted sprite before showing the deck back
   - Prevents duplicate animations with the `_isAnimating` flag

2. **Animation Timing:**
   - Pinch duration: 125ms (based on Balatro's 8 units/second rate)
   - Juice up duration: 300ms with elastic easing

3. **Resource Management:**
   - The behavior properly disposes subscriptions in `OnDetaching()`
   - Cache is cleared when the behavior detaches

4. **Deck Back Fallback:**
   - If deck back sprite is not found, skips animation and shows the reveal sprite directly

## Common Converter Types

The behavior works with all sprite converters in the codebase:

- `ItemNameToSpriteConverter` - Auto-detects item type
- `ShopItemSpriteConverter` - For ShopItemModel objects
- `BoosterPackSpriteConverter` - For booster packs
- `TagSpriteConverter` - For tags
- `StandardCardToSpriteConverter` - For playing cards
- `BossSpriteConverter` - For boss blinds
- `VoucherSpriteConverter` - For vouchers

## Animation Architecture

The behavior follows Balatro's original animation system:

1. **Pinch Animation** (card.lua:flip())
   - Linear easing at 8 units/second
   - Horizontal squeeze to invisible

2. **Sprite Swap**
   - Occurs at scaleX=0 (invisible moment)

3. **Unpinch Animation**
   - Linear expansion back to normal

4. **Juice Up** (moveable.lua:juice_up(0.3, 0.3))
   - Elastic bounce to 1.3x scale
   - Returns to 1.0x with elastic easing
   - Adds that Balatro "feel"
