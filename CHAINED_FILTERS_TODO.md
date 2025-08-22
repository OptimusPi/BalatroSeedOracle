# 🔗 Chained Filters Architecture - TODO

## 🎯 **Vision: Composable FilterDesc Chain**

Create separate FilterDesc classes for each filter type, then chain them conditionally based on JSON config.

## 🏗️ **Architecture Design**

### **Micro-FilterDescs**
```csharp
public struct VoucherFilterDesc : IMotelySeedFilterDesc<VoucherFilter>
public struct TarotFilterDesc : IMotelySeedFilterDesc<TarotFilter>  
public struct JokerFilterDesc : IMotelySeedFilterDesc<JokerFilter>
public struct SoulJokerFilterDesc : IMotelySeedFilterDesc<SoulJokerFilter>
public struct PlayingCardFilterDesc : IMotelySeedFilterDesc<PlayingCardFilter>
public struct BossFilterDesc : IMotelySeedFilterDesc<BossFilter>
```
### **Smart Conditional Chaining**
```csharp
// Only create filters that JSON config actually needs
var chain = new FilterChain();
if (config.HasVouchers) chain.Add(new VoucherFilterDesc(config.VoucherClauses));
if (config.HasTarots) chain.Add(new TarotFilterDesc(config.TarotClauses));
if (config.HasSoulJokers) chain.Add(new SoulJokerFilterDesc(config.SoulJokerClauses));

// Execute chain with early exit
VectorMask result = chain.Execute(ref searchContext);
```

### **Fluent API Integration**
```csharp
var search = new MotelySearchSettings(voucherFilter)
    .ChainWith(tarotFilter)
    .ChainWith(jokerFilter) 
    .WithFinalScoring(shouldClauses)
    .WithResultCallback((seed, score, details) => {
        Console.WriteLine($"{seed},{score}");
    })
    .Start();
```

## 🚀 **Benefits**

### **1. Skip Entire Filter Types**
- Voucher-only config: Only VoucherFilterDesc runs
- No unused filter overhead

### **2. Perfect Vectorization**
- Each FilterDesc optimized for its type only
- No compromises for other filter types

### **3. Clean Separation**
- No double processing
- No state confusion
- Single responsibility per filter

### **4. Performance Scaling**
- Simple configs: Skip 80%+ of logic
- Complex configs: Each step perfectly optimized

## 📋 **Implementation Plan**

### **Phase 1: Extract Filters**
1. Create VoucherFilterDesc.cs
2. Create TarotFilterDesc.cs  
3. Create JokerFilterDesc.cs
4. Create SoulJokerFilterDesc.cs

### **Phase 2: Chain Infrastructure**
1. Create FilterChain class
2. Add conditional chain building
3. Integrate with MotelySearchSettings

### **Phase 3: Result Handling**
1. Only final filter handles callbacks
2. Intermediate filters return VectorMask only
3. Clean result reporting

## 🎯 **Key Insights from Current Analysis**

### **Problems to Solve:**
- ❌ Double processing of Must clauses
- ❌ HashSet.Contains() killing SIMD
- ❌ Multiple List allocations per call
- ❌ String operations in hot path
- ❌ MustNot not vectorized

### **SIMD Opportunities:**
- ⚡ Bit masking for slot checking
- ⚡ Stack-allocated clause arrays  
- ⚡ Vectorized ante range validation
- ⚡ SIMD score calculation
- ⚡ Unified Must/MustNot processing

## 🚨 **Critical Design Decision**

**Current monolithic approach has accumulated too many architectural problems.**

**Recommendation**: Start fresh with clean FilterDesc architecture rather than retrofitting the existing hybrid mess.

---

**Next Steps**: Get current CSV output working, then implement clean chained FilterDesc architecture.