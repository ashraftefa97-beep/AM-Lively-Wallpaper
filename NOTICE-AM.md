# AM Lively Wallpaper

Based on Lively Wallpaper v0.9.6.0 by rocksdanister (Dani John).
Original project: https://github.com/rocksdanister/lively
Official source tag: https://github.com/rocksdanister/lively/tree/v0.9.6.0
Commit: 554bd7311b26953756ae9d11e5fe40c9d87f30bc

The supplied community release URL and official repository returned the same
v0.9.6.0 tag object/commit. There are no code differences at that tag.
Verification used both git ls-remote outputs and the fetched official tag.

Original Microsoft Public License and all original copyright notices remain.
See License.txt and src/livelywpf/livelywpf/docs/license.rtf for original and
third-party notices. AM additions are distributed under MS-PL.
AM is a derivative, not an official rocksdanister release or endorsement.
The AM monogram is newly drawn; existing icon resource identifiers are retained
for binary-resource compatibility but their image contents are replaced.
Upstream tutorial videos and wallpaper format names retain their attribution.

Changes: protected process management, live measured process data, editable
modes, explicit close and force approval, durable recovery journal, optional
restore on actual application exit, AM identity and installer, disabled upstream
self-update to retain Windows 8.1 targeting. Closing the window still minimizes
to the tray; actual Exit triggers restore. A shutdown or power loss cannot
promise completed recovery; pending entries remain for Restore All next launch.

External CefSharp/mpv binaries are not built from this repository. The Windows
build script takes the original v0.9.6.0 portable distribution and copies its
external directory byte-for-byte, preserving its own licenses. Optional mpv and
other runtimes absent from that original package must be supplied from an
existing verified Windows 8.1-compatible installation; no newer version is fetched.
