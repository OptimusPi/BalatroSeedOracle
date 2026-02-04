Motely FilterDesc Auditor & JAML Wiring
=======================================

Purpose
-------
Audit or add Motely FilterDesc implementations and wire them into the JAML/JAML JSON pipeline with a focus on SIMD hotpaths.

Use When
--------
- Adding a new filter to MotelyJson/JAML.
- Auditing filter performance or correctness.
- Wiring a criteria-based filter into the orchestration pipeline.

Inputs to Request
-----------------
- Filter category (item type) and desired semantics (must/should/mustNot).
- Inputs: value/values, edition, sources, antes, min thresholds.
- Expected SIMD behavior: SIMD-only vs SIMD+scalar verification.

Audit Checklist
---------------
- Identify the filter desc and criteria type.
- Verify hotpath:
  - No string comparisons in SIMD loops.
  - No LINQ in vector loops.
  - Uses cached streams if applicable (`ctx.Cache*`).
- Verify correctness:
  - Empty clauses guarded by `Debug.Assert` (programming error).
  - WantedAntes/EfficientAntes populated.
  - Must/should separation correct.
- Verify scalar verification usage:
  - Only used on reduced mask or rare paths.
  - Min thresholds handled correctly.

Wiring Checklist (JAML/JAML JSON)
---------------------------------
- Add alias to `MotelyJsonPerformanceUtils.TypeMap`.
- Parse enums/wildcards in `MotelyJsonConfig.PostProcess`.
- Add typed clause in `MotelyJsonFilterClauseTypes` with `FromJsonClause`.
- Add category routing in `FilterCategoryMapper.GetCategory`.
- Add desc creation in `SpecializedFilterFactory.CreateSpecializedFilter`.
- Ensure composite handling in `MotelyCompositeFilterDesc` if needed.
- Add scoring hooks if `should` uses tally/score.

Output Format
-------------
- Summary of files touched.
- SIMD vs scalar verification classification.
- Performance notes and any risks.
