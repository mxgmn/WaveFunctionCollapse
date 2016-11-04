
CSC=mcs

.PHONY: all
all: wfc.exe

install: wfc.exe
	cp wfc.exe $(DESTDIR)/wfc.exe

wfc.exe: Options.cs
	$(CSC) /debug /d:NDESK_OPTIONS /reference:System.Drawing.dll *.cs /out:$@

Options.cs:
	cp `pkg-config --variable=Sources mono-options` .

.PHONY: clean
clean:
	$(RM) wfc.exe Options.cs

