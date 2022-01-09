[![download]][Latest] [![l]](LICENSE)

Do-Re-Mi Lyrics is a free, open-source software for creating [enhanced lyrics](https://en.wikipedia.org/wiki/LRC_(file_format)#Enhanced_format). It supports many audio formats and it's easy to use. You can use it with karaoke software or with lyrics software, e.g. [MiniLyrics](https://www.crintsoft.com).

Download: [latest version][Latest]

How to use:
* open audio file
* open lyrics file or paste it from the clipboard
* start audio (Space or button)
* use F5 to set time at the start of every word
* use F6 to set ending time for previous line
* use F8 to delete last time
* use arrow keys and mouse to change current word
* red time means that it's earlier then the previous one and it should be corrected

![Do-Re-Mi Lyrics](https://user-images.githubusercontent.com/5322956/148644781-66c0b717-6c07-4ab4-b3a2-2b88bb669296.png)

Mouse shortcuts:
* Left Mouse Button - Highlight word
* Double Left Mouse Button - Change playing time to the current word minus 3 seconds
* Right Mouse Button - Divide word into parts (not working yet)

Keyboard shortcuts:
* F1 - About/Help/Changelog/License
* F2 - New lyrics
* F3 - Open audio file
* F4 - Open lyrics file
* F5 - Set time at the end of the previous word (if there's a longer gap between two words/lines)
* F6 - Set time at the beginning of the highlighted word
* F8 - Removes time from the previous word
* F12 - Edit text of the lyrics (not working yet)
* Ctrl+S - Save lyrics file
* Ctrl+Shift+S - Save lyrics to a new file
* Ctrl+V - Paste lyrics text from the clipboard
* Left - Highlight the previous word
* Right - Highlight the next word
* Up - Highlight the first word in the previous line
* Down - Highlight the first word in the next line
* Ctrl+Enter - Change playing time to the current word minus 3 seconds
* Enter - Move the current word to the next line
* Backspace - Move the current line to the previous one (works only when the first word in line is highlighted)
* Delete - Move the next line to the end of current one (works only when the last word in line is highlighted)
* Space - Play/Pause
* Ctrl+Left - Rewind 5 seconds
* Ctrl+Right - Fast forward 5 seconds
* Ctrl+Up - Increase tempo by 0.1
* Ctrl+Down - Decrease tempo by 0.1
* Shift+Up - Increase volume by 10%
* Shift+Down - Decrease volume by 10%

[Latest]: https://github.com/Woo-Cash/do-re-mi-lyrics/releases/latest "GitHub latest stable downloads"
[download]: https://img.shields.io/github/v/release/woo-cash/do-re-mi-lyrics?label=download
[l]: https://img.shields.io/badge/license-GPL3-blue.svg
