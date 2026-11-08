# AM Lively Wallpaper

[![Build Windows EXE](https://github.com/ashraftefa97-beep/AM-Lively-Wallpaper/actions/workflows/windows-build.yml/badge.svg)](https://github.com/ashraftefa97-beep/AM-Lively-Wallpaper/actions/workflows/windows-build.yml)

مصدر مشتق من Lively Wallpaper v0.9.6.0، يستهدف Windows 8.1 و.NET Framework 4.7.2.

## تحميل EXE
افتح [Actions](https://github.com/ashraftefa97-beep/AM-Lively-Wallpaper/actions/workflows/windows-build.yml)، ثم أحدث تشغيل ناجح، ثم حمّل `AM-Lively-Wallpaper-Win81-x86` من Artifacts. يحتوي المثبّت والنسخة المحمولة.

كل تعديل في main يشغّل بناء ويندوز تلقائيًا. يمكن تشغيله يدويًا من Run workflow. البناء يستخدم VS 2019 وNuGet 5.8.1 وInno Setup 6.2.2، مع نفس target framework ومكتبات التطبيق القديمة. نجاح البناء على Windows Server لا يثبت توافق التشغيل على Windows 8.1.

- [خطوات البناء والاستخدام بالعربي](BUILD-AR.md)
- [نتائج الاختبارات وحدود التسليم](TEST-RESULTS.md)
- [نسب المشروع والترخيص](NOTICE-AM.md)
- [README الأصلي](README-UPSTREAM.md)

ملفات فيديو الشرح الأصلية تُجلب تلقائيًا من commit المصدر المحدد وتُفحص بصمتها SHA256 قبل البناء. صور GIF الخاصة بعرض README الأصلي ليست مطلوبة لتشغيل التطبيق وموجودة في المستودع الأصلي. لا يُجلب كود أو محرك تشغيل من أحدث إصدار.
