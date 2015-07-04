/*
	This project is released under the GPL 2.0 license.
	Some parts are based on research by Bas Timmer and the OpenSteamworks project.
	Please do no evil.

	Initial author: (https://github.com/)Convery
	Started: 2014-11-28
	Notes:
		Find a pattern of bytes, nothing more to it.
*/

#pragma once

class Patterns
{
public:
	static uint8_t *FindPattern(const uint8_t *Pattern, const char *Mask);
	static uint8_t *FindPattern(uint8_t *BasePtr, uint8_t *EndPtr, const uint8_t *Pattern, const char *Mask);
};