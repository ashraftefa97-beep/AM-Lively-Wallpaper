"""Source-level checks only; does not claim to build or execute Windows/WPF."""
from pathlib import Path
import hashlib, json, xml.etree.ElementTree as ET
from tree_sitter import Language, Parser
import tree_sitter_c_sharp
root=Path(__file__).resolve().parents[1]
parser=Parser(Language(tree_sitter_c_sharp.language()))
count=0
def check(ok, message):
 global count
 if not ok: raise AssertionError(message)
 count+=1
 print('PASS',message)
baseline=json.loads((root/'tests/UPSTREAM-SHA256.json').read_text())
def unchanged(path): return hashlib.sha256((root/path).read_bytes()).hexdigest()==baseline['files'][path]
check(baseline['commit']=='554bd7311b26953756ae9d11e5fe40c9d87f30bc','pinned official release baseline')
check(unchanged('License.txt'),'original MS-PL license preserved byte-for-byte')
for f in (root/'src').rglob('packages.config'):
 check(unchanged(f.relative_to(root).as_posix()),'dependency pins unchanged: '+str(f.relative_to(root)))
ns={'m':'http://schemas.microsoft.com/developer/msbuild/2003'}
for f in (root/'src').rglob('*.csproj'):
 doc=ET.parse(f)
 check(doc.find('.//m:TargetFrameworkVersion',ns).text=='v4.7.2','framework unchanged: '+f.name)
for f in (root/'src').rglob('*'):
 if f.suffix in ('.xaml','.resx','.csproj'):
  ET.parse(f)
check(True,'all XAML, resources and projects parse as XML')
for f in (root/'src').rglob('*.cs'):
 check(not parser.parse(f.read_bytes()).root_node.has_error,'C# syntax: '+f.relative_to(root).as_posix())
p=root/'src/livelywpf/livelywpf'
doc=ET.parse(p/'livelywpf.csproj')
includes={e.attrib.get('Include') for e in doc.findall('.//m:Compile',ns)}
for f in (p/'ProcessManager').glob('*.cs'):check('ProcessManager\\'+f.name in includes,'new source included: '+f.name)
for f in ['wp_lib/Pause.cs','wp_lib/NativeMethods.cs','save/CefProperties.cs','preview/PreviewWallpaper.xaml.cs']:
 check(unchanged('src/livelywpf/livelywpf/'+f),'wallpaper subsystem preserved: '+f)
# Verify only product process identity was changed in the playback engine.
reversed_playback=(p/'wp_lib/SetupDesktop.cs').read_bytes().replace(b'"AM-Lively-Wallpaper"',b'"livelywpf"')
check(hashlib.sha256(reversed_playback).hexdigest()==baseline['files']['src/livelywpf/livelywpf/wp_lib/SetupDesktop.cs'],'playback engine changed only for renamed executable identity')
engine=(p/'ProcessManager/ProcessEngine.cs').read_text()
check('StartupType' not in engine and 'ChangeStartMode' not in engine,'no permanent service startup modification')
check('p.Kill()' in engine and 'forceApproved && !journal.Any' in engine,'force entrypoint requires a recorded graceful attempt')
check('ProcessTree' not in engine,'no process-tree termination')
check('p.StartTime.ToUniversalTime().Ticks!=current.Started' in engine,'PID reuse guard exists at action boundary')
check('s.Flush(true)' in engine and 'File.Replace' in engine,'recovery journal is flushed and atomically replaced')
print('PASS:',count,'source checks; Windows build/runtime not covered.')
