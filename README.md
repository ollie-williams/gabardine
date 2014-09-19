# Gabardine
This is the source code for the Gabardine project. The Gabardine code is managed, being written mostly in C#, although there is some F# and may be more in the future. Gabardine used to be called "Coconut". There is still some legacy C++ code in this directory in the Coconut namespace. This document will not describe how to build and run this largely defunct code base (for which you should be grateful as it was painful).

## Setup

### Dependencies
The majority of the dependencies for Gabardine will automatically be retrieved from NuGet when you build [```Gabardine.sln```](Gabardine.sln). For this reason please make sure you are connected to the Internet when you first build the solution.

There are some other dependencies that you will need to build and run the entire code base as it stands which are *not* automatically retrieved, nor are they in the repository:

1. In places some features from the new [Roslyn](http://roslyn.codeplex.com/) compiler platform have been used. I have been using the End User Preview as a plugin or Visual Studio 2013. It is possible that you will also be able to use [Visual Studio "14"](http://www.visualstudio.com/en-us/downloads/visual-studio-14-ctp-vs), but that hasn't been tested. Please follow the instructions at [http://roslyn.codeplex.com/](http://roslyn.codeplex.com/) to install.

2. One back-end Gabardine provides is via the LLVM compiler ```llc.exe```. Previously we built against the LLVM libraries which was a headache. Now we simply emit LLVM assembly and execute their tool, so all you need is the executable. See the [LLVM](http://llvm.org) homepage.

3. The last dependency is the [Intel MKL](https://software.intel.com/en-us/intel-mkl), which is what several of the demos are using to supply low-level matrix kernels.

### Settings file
In the root Gabardine directory you will see the file [```user_settings.json```](user_settings.json). This is a template file which tells Gabardine where the dependencies are located. The paths in this file should be modified to reflect your configuration. 

There are still some hard-wired paths here and there in the project. For this reason, you may find it easier to simply make your file system reflect what is already in the ```user_settings.json``` file, rather than the other way around. One way in which this is easily achieved is to create directly links via the command line with 

    mklink /d <softlink\directory> <actual\directory\location>

### Debugging directories
Visual Studio's debugging settings are local to your system, and are not under source control. Several of the Gabardine demo executables assume that they are running within the root Gabardine directory. By default, your freshly cloned version will not do that. You therefore need to edit the project settings for those projects to change the working directory. Several of the executable expect the path to ```user_settings.json``` as an argument.