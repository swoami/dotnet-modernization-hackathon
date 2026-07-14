# 05-final-validation: Attempt solution build and document known limitations

## Objective

Run full-solution restore + build, verify Common/Data build clean, classify every remaining error as a known unsupported-API limitation (not a conversion mistake), and produce the known-limitations report.

## Research / validation approach

- dotnet restore ContosoInsurance.sln -> must succeed for all 5 projects
- dotnet build ContosoInsurance.sln -> catalog errors per project, map each to an unsupported Framework API family
- No code changes in this task.
