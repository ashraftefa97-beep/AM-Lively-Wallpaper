# نتائج الاختبارات — 2026-11-08

## نُفذ بالفعل
- جلب الـtag الرسمي `refs/tags/v0.9.6.0`، وفحص تطابقه مع رابط community: `554bd7311b26953756ae9d11e5fe40c9d87f30bc`.
- يوجد branch بنفس اسم الـtag في المستودع؛ تم تثبيت المصدر على **مرجع الـtag الصريح**. الفرق بين branch والـtag كان ملف قالب GitHub issue فقط، لا كود التطبيق.
- ترجمة `SafetyPolicy.cs` واختباراته باستخدام Mono C# 6.8 على Linux، ثم تشغيل الاختبارات: **41 assertion نجحت**. شملت حماية النظام والأمان ومساعدي التطبيق، رفض الناشر المزيف، التطابق الدقيق للناشر، الخدمات التابعة، حدود المسارات، ومعالجة `..`.
- ترجمة `SafetyPolicy.cs` و`NativeTrust.cs` و`ProcessEngine.cs` إلى مكتبة C# باستخدام Mono: نجحت، دون أخطاء أو تحذيرات.
- **66 فحصًا للمصدر نجحت**: تحليل صياغة C#، XML لجميع XAML/resources/projects، إدراج الملفات الجديدة، بقاء net472 ونسخ NuGet، حفظ MS-PL، وفحوص الربط والحماية. السجل: `tests/source-checks.txt`.
- `git diff --check`: نجح.
- ثبات محرك الخلفيات: `SetupDesktop.cs` لم يتغير سوى مطابقة اسم ملف التطبيق الجديد؛ ملفات Pause وNativeMethods وCefProperties وPreviewWallpaper.xaml.cs محفوظة كما كانت. هذا تحقق مصدر، وليس اختبار تشغيل.

## لم يُنفذ — لا يوجد ادعاء بالنجاح
- لم تُبنَ واجهة WPF الكاملة ببرنامج Visual Studio/MSBuild Windows.
- لم يُنتج EXE للتطبيق أو مثبّت Windows.
- لم تُنفذ WinVerifyTrust أو WMI أو إدارة خدمات/عمليات Windows أثناء الاختبار.
- لم يُشغّل التطبيق على Windows 8.1، ولم تُختبر الخلفيات أو الشاشات المتعددة أو fullscreen أو الإيقاف/الاستعادة فعليًا.
- سكربت PowerShell والتثبيت Inno Setup يحتاجان التجربة على ويندوز.

Mono استُخدم فقط للتحقق من الكود المستقل عن الواجهة؛ لم يغيّر target framework أو مكتبات المنتج. قائمة قبول Windows 8.1 وخطوات البناء موجودة في BUILD-AR.md.

## إعادة الاختبارات
على ويندوز: `build\Test-Windows.ps1` باستخدام csc الخاص بـVisual Studio 2019؛ السكربت الرئيسي يستدعيه أيضًا.
فحوص المصدر الاختيارية تحتاج Python وحزم `tree-sitter` و`tree-sitter-c-sharp` ثم:
`python tests/verify_source.py`.
هذه الفحوص لا تقوم بإغلاق أي عملية أو خدمة.
