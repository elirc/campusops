# CampusOps workflow observation lab

This lab copies current domain source and selected real in-memory stores into a new external directory. Six bounded checks make chapters 03-10 executable. Five are current-behavior observations, including limitations; one checks the public cancellation boundary. A passing limitation observation is not a desired production acceptance rule.

Run from the CampusOps project root with Python 3 and the installed .NET 10 SDK. The generated project has no external package references and clears package sources. The reused generator refuses an existing output or a destination inside the application before creating files.

~~~powershell
$campusOpsWorkflowLab = Join-Path $env:TEMP ("campusops-workflow-" + [guid]::NewGuid().ToString("N"))
python astraupskill/advanced/extended-course/lab/generate_workflow_lab.py --output $campusOpsWorkflowLab
if ($LASTEXITCODE -ne 0) { throw "Lab generation failed; do not continue" }
Write-Output $campusOpsWorkflowLab
dotnet run --project (Join-Path $campusOpsWorkflowLab "Lab.csproj")
~~~

Expected unchanged-source outcome is six passes and exit zero. The cases cover additive repeated reservations, cancellation before and after a completed public call, conversion failure after request mutation, nonexclusive version checks, mutable skill collection exposure, and comment publication before a failed audit append.

The version case is an explicit domain-method interleaving, not a claimed HTTP concurrency reproduction. The cancellation case chooses public boundaries and does not claim to interrupt an internal instruction. The fault wrappers throw at named seams while delegating relevant reads to real stores. SOURCE-MAP.json records copied source hashes, including the additional workflow adapters.

Edit only the generated copy for repairs. The original application and earlier starter recipe remain unchanged. This harness starts no API listener, uses no database, verifies no route authorization, and establishes no restart or cross-process guarantee. Independent exercise designs outside these six cases remain learner work unless separately executed and recorded.

## Recorded verification

On 2026-09-29, the generated suite compiled and passed all six checks with exit zero. Separate attempts targeting an existing output and a path inside CampusOps were refused. The source and test baseline remained unchanged. These results establish the named bounded observations, not completion of the proposed feature designs or an executed HTTP concurrency test.

## Policy and integration suite

Pass the policy suite explicitly in a second new output. It adds four observations for procurement threshold equality, backward service-level resume, generated request length, and a webhook key retained after failed enqueue. The webhook case sends no network request and uses no secret. These checks characterize current boundaries, not implemented repairs.

~~~powershell
$campusOpsPolicyLab = Join-Path $env:TEMP ("campusops-policy-" + [guid]::NewGuid().ToString("N"))
python astraupskill/advanced/extended-course/lab/generate_workflow_lab.py --suite policy --output $campusOpsPolicyLab
if ($LASTEXITCODE -ne 0) { throw "Lab generation failed; do not continue" }
Write-Output $campusOpsPolicyLab
dotnet run --project (Join-Path $campusOpsPolicyLab "Lab.csproj")
~~~

Expected result is four passes. The suite copies the actual in-memory webhook and idempotency implementation and records that additional source hash. Its plan-list stub supplies a controlled plan, while generated requests use the real copied request model and store. A proposed database transaction, HTTP sender, or production identity provider remains outside this harness.
