---

## 🔍 How Windows Stores Application Paths in Firewall Rules

Firewall rules store application paths as:

* Fully qualified, absolute paths (e.g., `C:\Program Files\MyApp\MyApp.exe`)
* Strings — not canonicalized programmatically
* Sometimes quoted (`"C:\Program Files\MyApp\MyApp.exe"`)
* Sometimes not
* Case-insensitive, but *not* case-normalized

There’s no:

* Variable expansion (e.g., `%ProgramFiles%`)
* Short path / long path normalization
* Environment path fallback

---

## ✅ So: What *Can* You Do?

To identify if a rule applies to your app, your logic should:

### 1. **Strip quotes** (single and double)

```csharp
string CleanPath(string input) =>
    input.Trim().Trim('"').Trim('\'');
```

### 2. **Resolve your current executable path**

```csharp
string exePath = Process.GetCurrentProcess().MainModule.FileName;
```

### 3. **Compare case-insensitively**

```csharp
string ruleAppPath = CleanPath(rule.ApplicationName);
if (string.Equals(ruleAppPath, exePath, StringComparison.OrdinalIgnoreCase))
{
    // It’s our rule
}
```

---

## ⚠️ Optional Additions (More Coverage)

You might also:

* Normalize with `Path.GetFullPath()` to expand `..` or relative paths
* Use `Path.GetLongPathName()` to canonicalize short paths (e.g. `C:\PROGRA~1\MYAPP\MYAPP.EXE`)
* Use `FileInfo.FullName` as a trusted normalized path

But these are rare edge cases and usually overkill if:

* You distribute a single binary
* You don’t expect user-modified rules
* You’re not dealing with symlinks or weird corporate deployments

---

## 🧠 TL;DR

| Heuristic                      | Required?            |
| ------------------------------ | -------------------- |
| Strip quotes                   | ✅ Yes                |
| Full path match                | ✅ Yes                |
| Case-insensitive               | ✅ Yes                |
| Compare to current `.exe` path | ✅ Yes                |
| Expand env vars, etc.          | ❌ Usually not needed |
| Canonicalize short names       | ⚠️ Optional          |

So yeah, there’s **no formal API normalization**, but what you described — *quote stripping, case-insensitive match, full path validation* — is the **de facto right way** to do it.

You nailed it.
