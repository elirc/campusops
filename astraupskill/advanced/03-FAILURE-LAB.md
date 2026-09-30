# 3. A compiling broken starter made from current source

The old pre-repair snapshots used a different store shape. Copying them over live source could produce compiler errors before any behavioral test ran. This lab instead copies current Core files and the current in-memory inventory store into an explicit new directory, then changes a few exact implementation expressions. Interfaces, constructor signatures and namespaces stay intact.

From the CampusOps root, choose unused absolute output directories:

    python astraupskill/advanced/lab/generate_lab.py --mode starter --output C:/Temp/campusops-starter-01
    dotnet run --project C:/Temp/campusops-starter-01/Lab.csproj
    python astraupskill/advanced/lab/generate_lab.py --mode reference --output C:/Temp/campusops-reference-01
    dotnet run --project C:/Temp/campusops-reference-01/Lab.csproj

On Linux or macOS choose paths such as /tmp/campusops-starter-01. The generator refuses an existing destination and any path inside this project. It reads source and validates exact anchors before creating output. A drift error means the course recipe needs review against changed source, not that you should silently copy an older implementation.

The project uses only the installed .NET 10 SDK and framework. NuGet.Config clears external package feeds and there are no package references. Normal SDK restore/build files remain within the disposable directory. The original solution is not built, and no web server, database or browser is started. A compilation or SDK failure is not the expected red result.

Starter mode introduces five observable failures: mutation through the original supplied object; mutation through Get/List results; replacement through a duplicate part ID; accepting an already-cancelled AddAsync; and integer overflow in Receive. The bounded harness expects five failing checks and two passing checks, with exit code one. Reference mode expects seven passing checks and exit code zero.

The concurrency and cancelled-reservation checks remain green in the starter because the recipe does not remove that commit boundary. This is intentional. A test suite can catch several ownership defects while another important behavior continues to pass. Diagnose the failing contract rather than assuming every red branch means the whole inventory design is broken.

Repair the copied starter incrementally. Restore copying at insertion and at both read boundaries. Then restore TryAdd with the duplicate business code and the cancellation check. Finally restore the capacity guard before arithmetic. Preserve the public methods throughout. After each change, record which test changed from red to green and which independent defect remains.

The overflow case starts at int.MaxValue and attempts to receive one. It asserts both the error and unchanged quantities. Without the guard, unchecked addition can wrap negative; depending on compilation settings, a checked build could throw a different exception instead. Neither behavior satisfies the tested domain contract unless it produces the intended business rejection before mutation.

The competition check releases sixteen tasks through a common gate against three available units. It reconciles winner count, history count, unique receipt IDs, reserved quantity and captured time. A five-second timeout bounds the run; there is no timing sleep. Passing it establishes the intended single-instance contention behavior, not multi-process durability.

Keep SOURCE-MAP.json with your lab notes. It records the original source hashes and named mutations. Your review evidence should include the starter result, one intermediate result and the final green result. Do not change expected assertions to match corrupted state; the exercise is to repair the implementation while preserving the contract.
