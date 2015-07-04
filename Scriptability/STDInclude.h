#pragma once

#define _CRT_SECURE_NO_WARNINGS
#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <stdio.h>
#include <stdint.h>
#include <vector>
#include <fstream>
#include <memory>
#include <map>

#include <io.h>
#include <conio.h>
#include <direct.h>
#include <stdint.h>

#include "Utility\FileIO.h"
#include "Utility\Patterns.h"
#include "Utility\Hooking.h"
#include "Utility\VariadicString.h"

#include "Struct\T6.h"

#include "Addresses\Addresses.h"
#include "Functions\Functions.h"

#include "Patches\Patches.h"

#include "Global\Global.h"
#include "Export\Export.h"

// Unneeded warnings.
#pragma warning(disable:4244)			// Conversion from large type to smaller type can cause data loss.
