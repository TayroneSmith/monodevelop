BUNDLE_VERSION=$(VERSION)
MAC_APP_DIR=MonoDevelop.app
APP_NAME=MonoDevelop
MACOS=$(MAC_APP_DIR)/Contents/MacOS
MAC_APP_LIB_DIR=$(MACOS)/lib/monodevelop
ADDINS=$(MACOS)/lib/monodevelop/AddIns
EXTRAS=$(top_srcdir)/../extras
ARTIFACTS=$(top_srcdir)/../../artifacts
DEPENDENCIES=$(top_srcdir)/../../dependencies

# In which commit did the MonoDevelop version change? 00000000 if uncommitted
commit_of_last_version_change    := $(shell LANG=C git blame $(top_srcdir)/configure.in | grep AC_INIT | sed 's/ .*//')

# How many commits have passed since $(-commit-of-last-version-change)?
# "0" when commit hash is invalid (e.g. 00000000)
num_commits_since_version_change := $(shell LANG=C git rev-list --count $(commit_of_last_version_change)..HEAD)

PACKAGE_VERSION_MAJOR=0$(word 1, $(subst ., ,$(VERSION)))
PACKAGE_VERSION_MINOR=$(word 2, $(subst ., ,$(VERSION)))
PACKAGE_VERSION_REV=0$(word 3, $(subst ., ,$(VERSION)))
PACKAGE_VERSION_BUILD=$(num_commits_since_version_change)
PACKAGE_UPDATE_ID=$(shell echo $(PACKAGE_VERSION_MAJOR) $(PACKAGE_VERSION_MINOR) $(PACKAGE_VERSION_REV) $(PACKAGE_VERSION_BUILD) | awk '{printf "%d%02d%02d%04d",$$1,$$2,$$3,$$4}')

BUNDLED_FRAMEWORK_DIR=$(MAC_APP_DIR)/Contents/Frameworks/Mono.framework
FINAL_INSTALL_DIR=/Applications/$(MAC_APP_DIR)
MODERN_GTK_DIR=../../../../gtk

CLEANFILES = render.exe
#DISTCLEANFILES = 
EXTRA_DIST = dmg-bg.png DS_Store Info.plist.in make-dmg-bundle.sh render.exe

all: app

render.exe: render.cs
	gmcs -r:System.Drawing render.cs

dmg: render.exe mono-bundle
	./make-dmg-bundle.sh

monostub: monostub.m
	gcc -Wall -mmacosx-version-min=10.6 -m32 -o monostub monostub.m -framework AppKit

clean-local:
	rm -rf MonoDevelop.app
	rm -f MonoDevelop*.dmg

app: monostub
	@echo ""
	@echo "Creating directories in app bundle"
	@echo ""
	mkdir -p $(MAC_APP_DIR)/Contents/{MacOS,Resources}
	mkdir -p $(MAC_APP_DIR)/Contents/MacOS/{lib,share}
	mkdir -p $(MAC_APP_LIB_DIR)

#	@echo ""
#	@echo "Copying install prefix into app bundle"
#	@echo ""
#	@if [[ "$(prefix)" == /Library* ]] || [[ "$(prefix)" == /usr* ]]; then \
#		echo "WARNING: cannot merge system prefix $(prefix)"; \
#	else \
#		echo "cp -pR $(prefix)/* $(MAC_APP_DIR)/Contents/MacOS/"; \
#		cp -pR $(prefix)/* $(MAC_APP_DIR)/Contents/MacOS/; \
#	fi

	@echo ""
	@echo "Copying MonoDevelop into app bundle"
	@echo ""
	cp -pR ../AddIns $(MAC_APP_LIB_DIR)
	cp -pR ../data $(MAC_APP_LIB_DIR)
	cp -pR ../bin $(MAC_APP_LIB_DIR)
	mkdir -p ../locale
	cp -pR ../locale $(MAC_APP_DIR)/Contents/MacOS/share
	mkdir -p $(MAC_APP_DIR)/Contents/MacOS/share/monodevelop
	cp ../../COPYING $(MAC_APP_DIR)/Contents/MacOS/share/monodevelop/COPYING.LGPL2.1
	
	sed -e "s/@BUNDLE_VERSION@/$(BUNDLE_VERSION)/" -e "s/@ASSEMBLY_VERSION@/$(ASSEMBLY_VERSION)/" -e "s/@APP_NAME@/$(APP_NAME)/" -e "s|@RELEASE_ID@|$(PACKAGE_UPDATE_ID)|" Info.plist.in > $(MAC_APP_DIR)/Contents/Info.plist
	cp ../../theme-icons/Mac/*.icns $(MAC_APP_DIR)/Contents/Resources/
	cp monodevelop.orig monodevelop
	cp monodevelop.orig $(MACOS)/monodevelop
	cp mdtool.orig mdtool
	cp mdtool.orig $(MACOS)/mdtool
	cp MonoDevelopProperties.xml EditingLayout2.xml $(MAC_APP_DIR)/Contents/MacOS/lib/monodevelop/data/options

# copy monodevelop.pc and monodevelop-core-addins.pc to Contents/MacOS/lib/pkgconfig
	mkdir -p $(MACOS)/lib/pkgconfig
	cp ../../*.pc $(MACOS)/lib/pkgconfig

# Copy main/monodevelop.xml to MonoDevelop.app/Contents/MacOS/share/mime/packages
	mkdir -p $(MACOS)/share/{applications,mime,icons,locale,man}
	mkdir -p $(MACOS)/share/mime/packages
	cp ../../monodevelop.xml $(MACOS)/share/mime/packages
	cp ../../monodevelop.desktop $(MACOS)/share/applications

# Native launch scripts
	@echo ""
	@echo "Copying launch scripts into app bundle"
	@echo ""
	cp monostub $(MACOS)/MonoDevelop
	cp monostub $(MACOS)/mdtool

# Remove addins that don't work on OSX
	@echo ""
	@echo "Removing unsupported addins"
	@echo ""

	rm -rf  $(ADDINS)/GnomePlatform* \
	  $(ADDINS)/WindowsPlatform*

# remove stuff we don't need
	@echo ""
	@echo "Removing unneeded files"
	@echo ""

	rm -rf $(MACOS)/lib/monodevelop/bin/UnitTests.dll* \
	  $(MACOS)/lib/monodevelop/bin/TestResult.xml \
	  $(MACOS)/lib/monodevelop/bin/addin-db* \
	  $(MACOS)/lib/monodevelop/bin/local \
	  $(MACOS)/lib/monodevelop/bin/repository-cache* \
	  $(MACOS)/lib/monodevelop/bin/addins-setup.config \
	  $(MACOS)/lib/monodevelop/AddIns/MonoDevelop.TextTemplating/Mono.TextTemplating.Tests*

# add extras known to work on Mac, if they exist
	@echo ""
	@echo "Copying supported extras into app bundle"
	@echo ""

	@for ADDIN in MonoDevelop.Database; do \
		(ls $(EXTRAS)/$${ADDIN}/build/* > /dev/null 2>&1 && \
			echo "Copying $${ADDIN}" && \
			mkdir -p $(MAC_APP_LIB_DIR)/AddIns/$${ADDIN} && \
			cp -pR $(EXTRAS)/$${ADDIN}/build/* $(MAC_APP_LIB_DIR)/AddIns/$${ADDIN}/ ) \
		|| (echo "Skipping $${ADDIN}" && echo "$${ADDIN}" >> missing-stuff)  ; \
	done

# add the binary artifacts if they exist
	@echo ""
	@echo "Copying binary artifacts into app bundle"
	@echo ""

	mkdir -p $(MACOS)/lib/pkgconfig
	mkdir -p $(MACOS)/MonoDoc.app/
	test ! -d $(ARTIFACTS)/MonoDoc.app ||  cp -pR $(ARTIFACTS)/MonoDoc.app/* $(MACOS)/MonoDoc.app/
	test ! -d MDMonitor.app ||  cp -pR MDMonitor.app $(MACOS)

# update revision in updateinfo
	@echo ""
	@echo "Updating build information"
	@echo ""

	pushd ../..; make buildinfo; popd

	echo "a3140c14-ef90-4019-ae6c-9d93804d6611 $(PACKAGE_UPDATE_ID)" > "$(MACOS)/updateinfo"

	echo "Release ID: $$(PACKAGE_UPDATE_ID)" > "$(MACOS)/lib/monodevelop/bin/buildinfo"
	@if test -e ../bin/buildinfo; then cat ../bin/buildinfo >> "$(MACOS)/lib/monodevelop/bin/buildinfo"; fi

# touch the directory so that update dependencies work
	touch MonoDevelop.app

	@echo ""
	@echo "MONODEVELOP RELEASE: $(PACKAGE_UPDATE_ID)"
	@echo "$$(cat ../../vcrevision)"
	@if test -f missing-stuff; then echo ""; echo "Missing stuff:"; cat missing-stuff; fi
	@echo ""
	@rm -f missing-stuff

mono-bundle: app
#########################
# Bundle mono
	echo "Preparing mono bundle..."
	mkdir -p $(MAC_APP_DIR)/Contents/Frameworks

# Copy system mono framework
	echo "Copying system framework ($(CURRENT))"
	rm -rf $(MAC_APP_DIR)/Contents/Frameworks/Mono.framework $(DEPENDENCIES)/Mono.framework
	mkdir -p $(MAC_APP_DIR)/Contents/Frameworks/Mono.framework/Versions
	unzip -d $(DEPENDENCIES) $(DEPENDENCIES)/Mono.framework-2.10.12.zip
	CURRENT=$(shell readlink $(DEPENDENCIES)/Mono.framework/Versions/Current)
	cp -R $(DEPENDENCIES)/Mono.framework/Versions/$(CURRENT) $(MAC_APP_DIR)/Contents/Frameworks/Mono.framework/Versions
	ln -s Versions/Current/bin $(MAC_APP_DIR)/Contents/Frameworks/Mono.framework/Commands
	ln -s Versions/Current/include $(MAC_APP_DIR)/Contents/Frameworks/Mono.framework/Headers
	ln -s Versions/Current $(MAC_APP_DIR)/Contents/Frameworks/Mono.framework/Home
	ln -s Versions/Current/lib $(MAC_APP_DIR)/Contents/Frameworks/Mono.framework/Libraries
	ln -s Libraries/libmono.dylib $(MAC_APP_DIR)/Contents/Frameworks/Mono.framework/Mono
	ln -s Versions/Current/Resources $(MAC_APP_DIR)/Contents/Frameworks/Mono.framework/Resources
	ln -s $(CURRENT) $(MAC_APP_DIR)/Contents/Frameworks/Mono.framework/Versions/Current
#	./transplant_gtk.sh $(MODERN_GTK_DIR) $(BUNDLED_FRAMEWORK_DIR)/Versions/Current
	mkdir -p $(BUNDLED_FRAMEWORK_DIR)/Versions/Current/etc/pango
	cp pangorc $(BUNDLED_FRAMEWORK_DIR)/Versions/Current/etc/pango
	cp monodevelop $(MACOS)/monodevelop
	cp mdtool $(MACOS)/mdtool
	cp replacePath.sh $(MACOS)/replacePath.sh
	cp replacePathQuoted.sh $(MACOS)/replacePathQuoted.sh
	chmod +x $(MACOS)/monodevelop $(MACOS)/mdtool

# Relocate .exe.config, .dll.config, pixbuf loaders, and .pc files
	echo "Relocating config files"
	find $(BUNDLED_FRAMEWORK_DIR) -name '*.config' -exec ./copyToInputFile.sh {} \;
	find $(BUNDLED_FRAMEWORK_DIR) -name '*.pc' -exec ./copyToInputFile.sh {} \;
	find $(BUNDLED_FRAMEWORK_DIR) -name '*.loaders' -exec ./copyToInputFile.sh {} \;
	./copyToInputFile.sh $(BUNDLED_FRAMEWORK_DIR)/Versions/Current/etc/mono/config
	./copyToInputFile.sh $(BUNDLED_FRAMEWORK_DIR)/Versions/Current/etc/pango/pango.modules
	./copyToInputFile.sh $(BUNDLED_FRAMEWORK_DIR)/Versions/Current/etc/pango/pangorc
	cp $(DEPENDENCIES)/gdk-pixbuf.loaders.in $(BUNDLED_FRAMEWORK_DIR)/Versions/Current/etc/gtk-2.0
	rm -f $(MAC_APP_LIB_DIR)/bin/MonoDevelop.exe.config

# Relocate launcher scripts
	echo "Relocating launcher scripts"
	file $(BUNDLED_FRAMEWORK_DIR)/Versions/Current/bin/* | grep 'shell script' | awk -F: '{ print $$1 }' | xargs -n 1 ./copyToInputFile.sh
#
#########################
	
MonoDevelop.app.zip: mono-bundle
	zip -r9uyq MonoDevelop.app.zip MonoDevelop.app

update-png:
	/Applications/Inkscape.app/Contents/Resources/script --without-gui --export-png=`pwd`/dmg-bg.png `pwd`/dmg-bg.svg

.PHONY: app dmg
