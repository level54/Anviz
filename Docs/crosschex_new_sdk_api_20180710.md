**New SDK API**

No.：XX-XX-XX-XX

Date created：2018-01-19

Version No.：0.0.0.14Logs Changed

| **Date Changed** | **User** | **Description**                   | **Version** |     |
| ---------------- | -------- | --------------------------------- | ----------- | --- |
| 2018-01-19       | 胡小明   | 创建                              | 0.0.0.1     |     |
| 2018-03-28       | 戴云建   | 增加客户端模式,增加2.24--2.38接口 | 0.0.0.14    |     |
|                  |          |                                   |             |     |
|                  |          |                                   |             |     |
|                  |          |                                   |             |     |
|                  |          |                                   |             |     |
|                  |          |                                   |             |     |
|                  |          |                                   |             |     |
|                  |          |                                   |             |     |

**Content**

[1 Overview 9](#_Toc20025)

[1.1 Introduction 9](#_Toc5465)

[1.2 Overview of function 9](#_Toc23605)

[1.3 System logic structure 9](#_Toc10270)

[1.4 Brief introduction of interface functions 9](#_Toc17241)

[1.5 Message type 11](#_Toc4976)

[2 Description interface 13](#_Toc8642)

[2.1 CChex_Version 14](#_Toc3896)

[2.1.1 Description functions 14](#_Toc264)

[2.1.2 Request 14](#_Toc12977)

[2.1.3 Response 14](#_Toc10070)

[2.1.4 Sample 14](#_Toc4027)

[2.1.5 Notice 14](#_Toc6657)

[2.2 CChex_Init 14](#_Toc11641)

[2.2.1 Description functions 14](#_Toc9766)

[2.2.2 Request 14](#_Toc1302)

[2.2.3 Response 15](#_Toc9935)

[2.2.4 Sample 15](#_Toc31159)

[2.2.5 Notice 15](#_Toc15795)

[2.3 CChex_Start 15](#_Toc19672)

[2.3.1 Description functions 15](#_Toc6800)

[2.3.2 Request 15](#_Toc23202)

[2.3.3 Response 15](#_Toc3753)

[2.3.4 Sample 15](#_Toc1546)

[2.3.5 Notice 16](#_Toc21752)

[2.4 CChex_Stop 16](#_Toc15020)

[2.4.1 Description functions 16](#_Toc32007)

[2.4.2 Request 16](#_Toc23234)

[2.4.3 Response 16](#_Toc5529)

[2.4.4 Sample 16](#_Toc25009)

[2.4.5 Notice 16](#_Toc23937)

[2.5 CChex_Update 16](#_Toc2037)

[2.5.1 Description functions 17](#_Toc13409)

[2.5.2 Request 17](#_Toc23413)

[2.5.3 Response 17](#_Toc4643)

[2.5.4 Sample 17](#_Toc6029)

[2.5.5 Notice 18](#_Toc17252)

[2.6 CChex_GetNetConfig 18](#_Toc8781)

[2.6.1 Description functions 18](#_Toc23094)

[2.6.2 Request 18](#_Toc187)

[2.6.3 Response 18](#_Toc32219)

[2.6.4 Sample 19](#_Toc25116)

[2.6.5 Notice 19](#_Toc22887)

[2.7 CChex_SetNetConfig 19](#_Toc8847)

[2.7.1 Description functions 19](#_Toc10349)

[2.7.2 Request 19](#_Toc26497)

[2.7.3 Response 20](#_Toc6694)

[2.7.4 Sample 20](#_Toc19654)

[2.7.5 Notice 20](#_Toc18094)

[2.8 CChex_MsgGetByIdx 20](#_Toc3919)

[2.8.1 Description functions 20](#_Toc3917)

[2.8.2 Request 20](#_Toc19171)

[2.8.3 Response 20](#_Toc18918)

[2.8.4 Sample 21](#_Toc19006)

[2.8.5 Notice 21](#_Toc22786)

[2.9 CChex_MsgDelByIdx 21](#_Toc9601)

[2.9.1 Description functions 21](#_Toc7443)

[2.9.2 Request 21](#_Toc2272)

[2.9.3 Response 22](#_Toc20702)

[2.9.4 Sample 22](#_Toc3559)

[2.9.5 Notice 22](#_Toc12849)

[2.10 CChex_MsgAddNew 22](#_Toc9174)

[2.10.1 Description functions 22](#_Toc8527)

[2.10.2 Request 22](#_Toc10205)

[2.10.3 Response 23](#_Toc29880)

[2.10.4 Sample 23](#_Toc5187)

[2.10.5 Notice 23](#_Toc27706)

[2.11 CChex_MsgGetAllHead 23](#_Toc9662)

[2.11.1 Description functions 23](#_Toc17767)

[2.11.2 Request 24](#_Toc4225)

[2.11.3 Response 24](#_Toc17637)

[2.11.4 Sample 24](#_Toc27343)

[2.11.5 Notice 24](#_Toc25408)

[2.12 CChex_RebootDevice 25](#_Toc24851)

[2.12.1 Description functions 25](#_Toc277)

[2.12.2 Request 25](#_Toc10191)

[2.12.3 Response 25](#_Toc26068)

[2.12.4 Sample 25](#_Toc475)

[2.12.5 Notice 25](#_Toc9587)

[2.13 CChex_SetTime 25](#_Toc9679)

[2.13.1 Description functions 25](#_Toc11456)

[2.13.2 Request 26](#_Toc25485)

[2.13.3 Response 26](#_Toc28980)

[2.13.4 Sample 26](#_Toc13124)

[2.13.5 Notice 26](#_Toc26744)

[2.14 CChex_GetSNConfig 26](#_Toc24932)

[2.14.1 Description functions 26](#_Toc19705)

[2.14.2 Request 26](#_Toc26459)

[2.14.3 Response 27](#_Toc8259)

[2.14.4 Sample 27](#_Toc16148)

[2.14.5 Notice 27](#_Toc8436)

[2.15 CChex_DownloadAllRecords 27](#_Toc12172)

[2.15.1 Description functions 27](#_Toc22919)

[2.15.2 Request 27](#_Toc17151)

[2.15.3 Response 27](#_Toc22093)

[2.15.4 Sample 28](#_Toc23410)

[2.15.5 Notice 28](#_Toc17479)

[2.16 CChex_DeleteRecordInfo 28](#_Toc16555)

[2.16.1 Description functions 28](#_Toc1988)

[2.16.2 Request 28](#_Toc6874)

[2.16.3 Response 29](#_Toc4330)

[2.16.4 Sample 29](#_Toc10100)

[2.16.5 Notice 29](#_Toc9623)

[2.17 CChex_GetBasicConfigInfo 29](#_Toc8730)

[2.17.1 Description functions 29](#_Toc21310)

[2.17.2 Request 29](#_Toc27006)

[2.17.3 Response 29](#_Toc4262)

[2.17.4 Sample 30](#_Toc26788)

[2.17.5 Notice 30](#_Toc5820)

[2.18 CChex_SetBasicConfigInfo 30](#_Toc20206)

[2.18.1 Description functions 30](#_Toc28885)

[2.18.2 Request 30](#_Toc13786)

[2.18.3 Response 31](#_Toc2062)

[2.18.4 Sample 31](#_Toc2394)

[2.18.5 Notice 31](#_Toc19122)

[2.19 CChex_ListPersonInfo 32](#_Toc28708)

[2.19.1 Description functions 32](#_Toc22478)

[2.19.2 Request 32](#_Toc7037)

[2.19.3 Response 32](#_Toc6232)

[2.19.4 Sample 33](#_Toc25152)

[2.19.5 Notice 33](#_Toc14993)

[2.20 CChex_ModifyPersonInfo 33](#_Toc26928)

[2.20.1 Description functions 33](#_Toc27027)

[2.20.2 Request 33](#_Toc27354)

[2.20.3 Response 34](#_Toc10788)

[2.20.4 Sample 34](#_Toc918)

[2.20.5 Notice 34](#_Toc20292)

[2.21 CChex_DeletePersonInfo 35](#_Toc19037)

[2.21.1 Description functions 35](#_Toc19477)

[2.21.2 Request 35](#_Toc1430)

[2.21.3 Response 35](#_Toc5639)

[2.21.4 Sample 35](#_Toc776)

[2.21.5 Notice 35](#_Toc8565)

[2.22 CChex_DownloadFingerPrint 36](#_Toc5262)

[2.22.1 Description functions 36](#_Toc5140)

[2.22.2 Request 36](#_Toc32580)

[2.22.3 Response 36](#_Toc25565)

[2.22.4 Sample 36](#_Toc22853)

[2.22.5 Notice 37](#_Toc3683)

[2.23 CChex_UploadFingerPrint 37](#_Toc29237)

[2.23.1 Description functions 37](#_Toc13231)

[2.23.2 Request 37](#_Toc1314)

[2.23.3 Response 37](#_Toc30996)

[2.23.4 Sample 37](#_Toc9871)

[2.23.5 Notice 38](#_Toc31646)

[2.24 CChex_GetTime 38](#_Toc14236)

[2.24.1 Description functions 38](#_Toc5901)

[2.24.2 Request 38](#_Toc12203)

[2.24.3 Response 38](#_Toc11633)

[2.24.4 Sample 38](#_Toc22514)

[2.24.5 Notice 39](#_Toc15589)

[2.25 CChex_InitUserArea 39](#_Toc12506)

[2.25.1 Description functions 39](#_Toc15363)

[2.25.2 Request 39](#_Toc14108)

[2.25.3 Response 39](#_Toc15105)

[2.25.4 Sample 39](#_Toc12025)

[2.25.5 Notice 39](#_Toc30351)

[2.26 CChex_InitSystem 40](#_Toc3585)

[2.26.1 Description functions 40](#_Toc13052)

[2.26.2 Request 40](#_Toc8261)

[2.26.3 Response 40](#_Toc10538)

[2.26.4 Sample 40](#_Toc11242)

[2.26.5 Notice 40](#_Toc23628)

[2.27 CChex_GetBasicConfigInfo2 40](#_Toc11889)

[2.27.1 Description functions 41](#_Toc9873)

[2.27.2 Request 41](#_Toc26935)

[2.27.3 Response 41](#_Toc31779)

[2.27.4 Sample 42](#_Toc24310)

[2.27.5 Notice 42](#_Toc7973)

[2.28 CChex_SetBasicConfigInfo2 42](#_Toc12881)

[2.28.1 Description functions 42](#_Toc5268)

[2.28.2 Request 42](#_Toc5547)

[2.28.3 Response 43](#_Toc26932)

[2.28.4 Sample 43](#_Toc16095)

[2.28.5 Notice 43](#_Toc3831)

[2.29 CChex_GetPeriodTime 43](#_Toc9621)

[2.29.1 Description functions 43](#_Toc7802)

[2.29.2 Request 43](#_Toc18749)

[2.29.3 Response 44](#_Toc25998)

[2.29.4 Sample 44](#_Toc25600)

[2.29.5 Notice 44](#_Toc13032)

[2.30 CChex_SetPeriodTime 44](#_Toc23081)

[2.30.1 Description functions 44](#_Toc4201)

[2.30.2 Request 44](#_Toc29134)

[2.30.3 Response 45](#_Toc31936)

[2.30.4 Sample 45](#_Toc14726)

[2.30.5 Notice 45](#_Toc12048)

[2.31 CChex_GetTeamInfo 45](#_Toc17744)

[2.31.1 Description functions 45](#_Toc28330)

[2.31.2 Request 46](#_Toc31805)

[2.31.3 Response 46](#_Toc3283)

[2.31.4 Sample 46](#_Toc27791)

[2.31.5 Notice 46](#_Toc8337)

[2.32 CChex_SetTeamInfo 46](#_Toc13275)

[2.32.1 Description functions 46](#_Toc12604)

[2.32.2 Request 46](#_Toc965)

[2.32.3 Response 47](#_Toc6871)

[2.32.4 Sample 47](#_Toc7566)

[2.32.5 Notice 47](#_Toc8664)

[2.33 CCHex_AddFingerprintOnline 47](#_Toc799)

[2.33.1 Description functions 47](#_Toc6792)

[2.33.2 Request 47](#_Toc20865)

[2.33.3 Response 48](#_Toc26669)

[2.33.4 Sample 48](#_Toc6100)

[2.33.5 Notice 48](#_Toc31373)

[2.34 CCHex_ForcedUnlock 48](#_Toc22343)

[2.34.1 Description functions 48](#_Toc25749)

[2.34.2 Request 49](#_Toc31658)

[2.34.3 Response 49](#_Toc15651)

[2.34.4 Sample 49](#_Toc22909)

[2.34.5 Notice 49](#_Toc17228)

[2.35 CCHex_Udp_Search_Dev 49](#_Toc26043)

[2.35.1 Description functions 49](#_Toc18828)

[2.35.2 Request 49](#_Toc15531)

[2.35.3 Response 50](#_Toc18375)

[2.35.4 Sample 51](#_Toc30841)

[2.35.5 Notice 51](#_Toc12804)

[2.36 CCHex_Udp_Set_Dev_Config 51](#_Toc7532)

[2.36.1 Description functions 51](#_Toc25159)

[2.36.2 Request 51](#_Toc20720)

[2.36.3 Response 52](#_Toc5623)

[2.36.4 Sample 52](#_Toc31183)

[2.36.5 Notice 52](#_Toc17327)

[2.37 CCHex_ClientConnect 53](#_Toc6987)

[2.37.1 Description functions 53](#_Toc29809)

[2.37.2 Request 53](#_Toc2556)

[2.37.3 Response 53](#_Toc25490)

[2.37.4 Sample 53](#_Toc20634)

[2.37.5 Notice 54](#_Toc726)

[2.38 CCHex_ClientDisconnect 54](#_Toc3099)

[2.38.1 Description functions 54](#_Toc11409)

[2.38.2 Request 54](#_Toc466)

[2.38.3 Response 54](#_Toc23507)

[2.38.4 Sample 55](#_Toc1905)

[2.38.5 Notice 55](#_Toc17124)

[3 Return 55](#_Toc21525)

# Overview

## Introduction

This API document mainly defines the development interface of the Anviz SDK, which provides reference for developers to connect and operate related devices.

This document is mainly related to the software development engineers, software testing engineers and project managers.

## Overview of functions

This API document mainly provides some interfaces of the client/server to the PC end, getting the parameters of the time attendance/access control, and setting the configuration and parameters of the time attendance/access control device. In order to support multiple devices, the interfaces between queries and settings exist asynchronously. SDK is compiled into the form of a dynamic library.

## System logic structure

Device

PC

CChex_Init

CChex_Start

CChex_Stop

Start

CChex_GetNetConfig

CChex_SetNetConfig

CChex_Update

SDK

SetNetwork

GetNetwork

Result

Stop

## Brief introduction of interface functions

uint CChex_Version();

void CChex_Init();

IntPtr CChex_Start();

void CChex_Stop(IntPtr CchexHandle);

int CChex_Update(IntPtr CchexHandle, int\[\] DevIdx, int\[\] Type, IntPtr Buff, int Len);

int CChex_GetNetConfig(IntPtr CchexHandle, int DevIdx);

int CChex_SetNetConfig(IntPtr CchexHandle, int DevIdx, ref CCHEX_NETCFG_INFO_STRU Config);

int CChex_MsgGetByIdx(IntPtr CchexHandle, int DevIdx, byte Idx);

int CChex_MsgDelByIdx(IntPtr CchexHandle, int DevIdx, byte Idx);

int CChex_MsgAddNew(IntPtr CchexHandle, int DevIdx, byte\[\] Data, int Len);

int CChex_MsgGetAllHead(IntPtr CchexHandle, int DevIdx);

int CChex_RebootDevice(IntPtr CchexHandle, int DevIdx);

int CChex_SetTime(IntPtr CchexHandle, int DevIdx, int Year, int Month, int Day, int Hour, int Min, int Sec);

int CChex_GetSNConfig(IntPtr CchexHandle, int DevIdx);

int CChex_DownloadAllRecords(IntPtr CchexHandle, int DevIdx);

int CChex_DeleteRecordInfo(IntPtr CchexHandle, int DevIdx, ref CCHEX_DEL_RECORD_INFO_STRU Config);

int CChex_GetBasicConfigInfo(IntPtr CchexHandle, int DevIdx);

int CChex_SetBasicConfigInfo(IntPtr CchexHandle, int DevIdx, ref CCHEX_SET_BASIC_CFG_INFO_STRU Config);

int CChex_ListPersonInfo(IntPtr CchexHandle, int DevIdx);

int CChex_ModifyPersonInfo(IntPtr CchexHandle, int DevIdx, ref CCHEX_RET_PERSON_INFO_STRU personlist, byte person_num);

int CChex_DeletePersonInfo(IntPtr CchexHandle, int DevIdx, ref CCHEX_DEL_PERSON_INFO_STRU Config);

int CChex_DownloadFingerPrint(IntPtr CchexHandle, int DevIdx, byte\[\] EmployeeId, byte FingerIdx);

int CChex_UploadFingerPrint(IntPtr CchexHandle, int DevIdx, byte\[\] EmployeeId, byte FingerIdx, byte\[\] FingerData, int DataLen);

int CChex_GetTime(IntPtr CchexHandle, int DevIdx);//(24)

int CChex_InitUserArea(IntPtr CchexHandle, int DevIdx);

int CChex_InitSystem(IntPtr CchexHandle, int DevIdx);

int CChex_GetBasicConfigInfo2(IntPtr CchexHandle, int DevIdx);

int CChex_SetBasicConfigInfo2(IntPtr CchexHandle, int DevIdx, ref CCHEX_GET_BASIC_CFG_INFO2_STRU_EXT_INF config);

int CChex_GetPeriodTime(IntPtr CchexHandle, int DevIdx, byte SerialNumbe);

int CChex_SetPeriodTime(IntPtr CchexHandle, int DevIdx, ref CCHEX_SET_PERIOD_TIME_STRU_EXT_INF config);

int CChex_GetTeamInfo(IntPtr CchexHandle, int DevIdx, byte TeamNumbe);

int CChex_SetTeamInfo(IntPtr CchexHandle, int DevIdx, ref CCHEX_SET_TEAM_INFO_STRU_EXT_INF config);

int CCHex_AddFingerprintOnline(IntPtr CchexHandle, int DevIdx, ref CCHEX_ADD_FINGERPRINT_ONLINE_STRU_EXT_INF Param);

int CCHex_ForcedUnlock(IntPtr CchexHandle, int DevIdx, ref CCHEX_FORCED_UNLOCK_STRU_EXT_INF Param);

int CCHex_Udp_Search_Dev(IntPtr CchexHandle);

int CCHex_Udp_Set_Dev_Config(IntPtr CchexHandle, ref CCHEX_UDP_SET_DEV_CONFIG_STRU_EXT_INF config);

int CCHex_ClientConnect(IntPtr CchexHandle, byte\[\] Ip, int Port);

int CCHex_ClientDisconnect(IntPtr CchexHandle, int DevIdx);

## Message Type

CChex_Update(anviz_handle, dev_idx, Type, pBuff, len);

You need to loop the query to see if there is an asynchronous return, and if you have a different Type of data parsing, get the data you want.public enum MsgType:int

{

CCHEX_RET_RECORD_INFO_TYPE = 1

, CCHEX_RET_DEV_LOGIN_TYPE

, CCHEX_RET_DEV_LOGOUT_TYPE

, CCHEX_RET_DLFINGERPRT_TYPE = 4

, CCHEX_RET_ULFINGERPRT_TYPE

, CCHEX_RET_MODIFY_PERSON_INFO_TYPE = 8

, CCHEX_RET_LIST_PERSON_INFO_TYPE

, CCHEX_RET_MSGGETBYIDX_INFO_TYPE = 12

, CCHEX_RET_MSGGETBYIDX_UNICODE_INFO_TYPE

, CCHEX_RET_MSGADDNEW_INFO_TYPE

, CCHEX_RET_MSGADDNEW_UNICODE_INFO_TYPE

, CCHEX_RET_MSGDELBYIDX_INFO_TYPE // 15

, CCHEX_RET_MSGGETALLHEAD_INFO_TYPE

, CCHEX_RET_REBOOT_TYPE

, CCHEX_RET_DEV_STATUS_TYPE

, CCHEX_RET_MSGGETALLHEADUNICODE_INFO_TYPE

, CCHEX_RET_SETTIME_TYPE // 20

, CCHEX_RET_UPLOADFILE_TYPE

, CCHEX_RET_GETNETCFG_TYPE

, CCHEX_RET_SETNETCFG_TYPE

, CCHEX_RET_GET_SN_TYPE //24

, CCHEX_RET_GET_BASIC_CFG_TYPE = 29

, CCHEX_RET_SET_BASIC_CFG_TYPE

, CCHEX_RET_DEL_PERSON_INFO_TYPE = 31

, CCHEX_RET_DEL_RECORD_OR_FLAG_INFO_TYPE = 33

,CCHEX_RET_GET_BASIC_CFG2_TYPE = 37

,CCHEX_RET_SET_BASIC_CFG2_TYPE = 38

,CCHEX_RET_GETTIME_TYPE = 39

,CCHEX_RET_INIT_USER_AREA_TYPE = 40

,CCHEX_RET_INIT_SYSTEM_TYPE = 41

,CCHEX_RET_GET_PERIOD_TIME_TYPE = 42

,CCHEX_RET_SET_PERIOD_TIME_TYPE = 43

,CCHEX_RET_GET_TEAM_INFO_TYPE = 44

,CCHEX_RET_SET_TEAM_INFO_TYPE = 45

,CCHEX_RET_ADD_FINGERPRINT_ONLINE_TYPE = 46

,CCHEX_RET_FORCED_UNLOCK_TYPE = 47

,CCHEX_RET_UDP_SEARCH_DEV_TYPE = 48

,CCHEX_RET_UDP_SET_DEV_CONFIG_TYPE = 49

,CCHEX_RET_CLINECT_CONNECT_TYPE = 200

,CCHEX_RET_CLINECT_DISCONNECT_TYPE = 201

};

# Description interface

## CChex_Version

### Description functions

【Function】Query SDK Version.

### Request

【Mode】uint CChex_Version()

【Parameter】None

### Response

【Return value】Get SDK version, Returns the version number of an integer number.

### Sample

int sdk_ver = CChex_Version();

### Notice

## CChex_Init

### Description functions

【Function】 The SDK function initializes and initializes the socket application interface.

### Request

【Mode】void CChex_Init()

【Parameter】None

### Response

【Return value】None

### Sample

CChex_Init();

### Notice

None

## CChex_Start

### Description functions

【Function】 Start the SDK, allocate space, and establish communication with the Anviz device.

### Request

【Mode】IntPtr CChex_Start()

【Parameter】None

### Response

【Return value】Return the handle of SDK.

### Sample

IntPtr sdk_handle = CChex_Start();

if (sdk_handle != null)

{

MessageBox.Show("Startup OK");

｝ else

{

MessageBox.Show("Startup errors,Please restart the program.");

}

### Notice

- Make sure that CChex_Init is initialized before run.

## CChex_Stop

### Description functions

【Function】Stop the SDK, release the space, close the socket chain and so on.

### Request

【Mode】void CChex_Stop(IntPtr CchexHandle)

【Parameter】CchexHandle,CChex_Start successfully create the handle

### Response

【Return value】None

### Sample

CChex_stop();

### Notice

- Make sure that CChex_Start has been started successfully before run.

## CChex_Update

### Description functions

【Function】The Return value get or set asynchronously.。

### Request

【Mode】int CChex_Update(IntPtr CchexHandle, int\[\] DevIdx, int\[\] Type, IntPtr Buff, int Len);

【Parameter】CchexHandle,CChex_Start successfully create the handle，Input\[Parameter\]；

DevIdx,Device index returned asynchronously,Output\[Parameter\]；

Buff,Returned data section,Output \[Parameter\]；

Len,Returns the length of the data section,Input\[Parameter\]；

### Response

【Return value】> 0：Successful asynchronous ; Return value == 0：Invalid Return value；< 0：buffer space is not enough，based on Return value, then re-apply the space.

### Sample

int ret = CChex_Update(anviz_handle, dev_idx, Type, pBuff, len);

if (ret > 0)

{

switch(Type)

{

case CCHEX_RET_DEV_LOGIN_TYPE: // Connect the Anviz device successfully and return the Device ID, Device IP address, Software version, Device type and software version, etc.

break;

case CCHEX_RET_GET_BASIC_CFG_TYPE: // Some basic information about the Anviz device, such as administrator password, firmware version, volume, etc.

break;

case CCHEX_RET_DEV_STATUS_TYPE: // Total number of employees, total number of fingerprints, total number of passwords, total number of cards, total attendance records, total new attendance record.

default:

break;

}

}else if (ret == 0)

{

// invalid data

}else

{

// Buff is not enough,

}

### Notice

1.Make sure that CChex_Start has been started successfully before run.

## CChex_GetNetConfig

### Description functions

【Function】Query Anviz device's network Configuration

### Request

【Mode】int CChex_GetNetConfig(IntPtr CchexHandle, int DevIdx);

【Parameter】CChex_Start successfully create the handle，Input\[Parameter\]；

DevIdx,search the device Input\[Parameter\]；

### Response

【Return value】1：The command was executed successfully.；Minus：Execute command failure

【Actual Data】CChex_Update, Type return CCHEX_RET_GETNETCFG_TYPE

Data Part：

uint MachineId; // Device ID

int Result; // 0:OK, -1:error

byte \[4\]IpAddr;// IP address

byte \[4\]IpMask;// Mask

byte \[6\]MacAddr;// MAC

byte \[4\]GwAddr;// Gateway

byte \[4\]ServAddr;// Sever IP address

byte RemoteEnable;//Standby

byte \[2\]Port;// Port

byte Mode;// Mode：0: Server，1:Client

byte DhcpEnable;// DHCP，0:Disable，1:Enable

### Sample

int ret = CChex_GetNetConfig();

### Notice

- Make sure that CChex_Start has been started successfully before run.

## CChex_SetNetConfig

### Description functions

【Function】Set the network configuration of the Anviz device.

### Request

【Mode】int CChex_SetNetConfig(IntPtr CchexHandle, int DevIdx, ref CCHEX_NETCFG_INFO_STRU Config);

【Parameter】CchexHandle：,CChex_Start successfully create the handle，Input\[Parameter\]；

DevIdx,Search the device，Input\[Parameter\]；

Config:

byte \[4\]IpAddr;// IP Address

byte \[4\]IpMask;// Mask

byte \[6\]MacAddr;// MAC address

byte \[4\]GwAddr;// Gateway

byte \[4\]ServAddr;// Server IP

byte RemoteEnable;//Standby

byte \[2\]Port;// Port

byte Mode;// Mode：0:Server，1:Client

byte DhcpEnable;// DHCP，0:Disable，1:Enable

### Response

【Return value】1：The command was executed successfully.；Minus：Execute command failure

【Actual Data】CChex_Update,Type return CCHEX_RET_SETNETCFG_TYPE

Data part：

uint MachineId; // Device ID

int Result; // 0:OK, -1:error

### Sample

int ret = (anviz_handle, dev_idx, ref dev_info);

### Notice

1.Make sure that CChex_Start has been started successfully before run.

## CChex_MsgGetByIdx

### Description functions

【Function】Reads the designated short message's start date, end date, and message content .

### Request

【Mode】int CChex_MsgGetByIdx(IntPtr CchexHandle, int DevIdx, byte Idx);

【Parameter】CchexHandle：,CChex_Start successfully create the handle，Input\[Parameter\]；

DevIdx,Search the device，Input\[Parameter\]；

Idx,Read the short message, Input\[Parameter\]；

### Response

【Return value】1：The command was executed successfully.；Minus：Execute command failure

【Actual Data】CChex_Update, Type return CCHEX_RET_MSGGETBYIDX_UNICODE_INFO_TYPE

Data part：

uint MachineId; // Device ID

int Result; // 0:OK, -1:error

int Len; // The length of the following data

byte\[5\] EmployeeId; // User ID，0 means public short message

byte StartYear; // Start time Year

byte StartMonth; // Start time Month

byte StartDay; // Start time Day

byte EndYear; // End time Year

byte EndMonth; // End time Month

byte EndDay; // End time Day

byte \[48\*2\]content // Content of short message,UNICODE

### Sample

int ret = CChex_MsgGetByIdx(anviz_handle, dev_idx, msg_idx);

### Notice

1.Make sure that CChex_Start has been started successfully before run.

## CChex_MsgDelByIdx

### Description functions

【Function】Delete the designated short message

### Request

【Mode】int CChex_MsgDelByIdx(IntPtr CchexHandle, int DevIdx, byte Idx);

【Parameter】CChex_Start， successfully create the handle，Input\[Parameter\]；

DevIdx,Search the device，Input\[Parameter\]；

Idx,Delete the designed short message,Input\[Parameter\]

### Response

【Return value】1：The command was executed successfully.；Minus：Execute command failure

【Actual Data】CChex_Update, Type return CCHEX_RET_MSGDELBYIDX_INFO_TYPE

Data Part：

uint MachineId; // Device ID

int Result; // 0:OK, -1:error

### Sample

int ret =CChex_MsgDelByIdx(anviz_handle, dev_idx, msg_idx);

### Notice

- Make sure that CChex_Start has been started successfully before run.

## CChex_MsgAddNew

### Description functions

【Function】Add short message.

### Request

【Mode】int CChex_MsgAddNew(IntPtr CchexHandle, int DevIdx, byte\[\] Data, int Len);

【Parameter】CChex_Start，successfully create the handle，Input\[Parameter\]；

DevIdx,Search the device，Input\[Parameter\]；

Data,Short message, Input\[Parameter\]；

byte\[5\] EmployeeId; // User ID，0 means public short message

byte StartYear; // Start time Year

byte StartMonth; // Start time Month

byte StartDay; // Start time Day

byte EndYear; // End time Year

byte EndMonth; // End time Month

byte EndDay; // End time Day

byte \[48\*2\]content // Content of short message,UNICODE

Len,the length of short message，Input\[Parameter\]；

### Response

【Return value】1：The command was executed successfully.；Minus：Execute command failure

【Actual Data】CChex_Update, Type return CCHEX_RET_MSGADDNEW_UNICODE_INFO_TYPE

Data Part：

uint MachineId; // Device ID

int Result; // 0:OK, -1:error

### Sample

int ret = CChex_MsgAddNew(anviz_handle, dev_idx, send_buff, send_len);

### Notice

- Make sure that CChex_Start has been started successfully before run.

## CChex_MsgGetAllHead

### Description functions

【Function】Read all short message headers.

### Request

【Mode】int CChex_MsgGetAllHead(IntPtr CchexHandle, int DevIdx);

【Parameter】CchexHandle：,successfully create the handle，Input\[Parameter\]；

DevIdx,Search the device，Input\[Parameter\]；

### Response

【Return value】1：The command was executed successfully.；Minus：Execute command failure

【Actual Data】CChex_Update, Type return CCHEX_RET_MSGADDNEW_UNICODE_INFO_TYPE Data Part：

uint MachineId; // Device ID

int Result; // 0:OK, -1:error

int Len; // All 50 SMS users, start and end time.

byte\[5\] EmployeeId; // User ID，0 means public SMS

byte StartYear; // Start time Year

byte StartMonth; // Start time Month

byte StartDay; // Start time Day

byte EndYear; // End time Year

byte EndMonth; // End time Month

byte EndDay; // End time Day

...Total 50 records

### Sample

int ret =CChex_MsgGetAllHead(anviz_handle, dev_idx);

### Notice

1.Make sure that CChex_Start has been started successfully before run.已经成功启动；

## CChex_RebootDevice

### Description functions

【Function】Restart the Anviz device

### Request

【Mode】int CChex_RebootDevice(IntPtr CchexHandle, int DevIdx);

【Parameter】CchexHandle：,successfully create the handle，Input\[Parameter\]；

DevIdx,Search the device，Input\[Parameter\]；

### Response

【Return value】1：The command was executed successfully.；Minus：Execute command failure

【Actual Data】CChex_Update, Type return CCHEX_RET_MSGADDNEW_UNICODE_INFO_TYPE Data Part：

uint MachineId; // Device ID

int Result; // 0:OK, -1:error

### Sample

int ret = CChex_RebootDevice(anviz_handle, dev_idx);

### Notice

1.Make sure that CChex_Start has been started successfully before run.已经成功启动；

## CChex_SetTime

### Description functions

【Function】Set date/time of the Anviz device

### Request

【Mode】int CChex_SetTime(IntPtr CchexHandle, int DevIdx, int Year, int Month, int Day, int Hour, int Min, int Sec);

【Parameter】CchexHandle：,successfully create the handle，Input\[Parameter\]；

DevIdx,Search the device，Input\[Parameter\]；

Year、Month、Day、Hour、 Min、 Sec，Input\[Parameter\]

### Response

【Return value】1：The command was executed successfully.；Minus：Execute command failure

【Actual Data】CChex_Update, Type return CCHEX_RET_MSGADDNEW_UNICODE_INFO_TYPE

Data Part：

uint MachineId; // Device ID

int Result; // 0:OK, -1:error

### Sample

int ret = CChex_SetTime(anviz_handle, dev_idx, 2018, 1, 25, 10, 10, 10);

### Notice

1.Make sure that CChex_Start has been started successfully before run.已经成功启动；

## CChex_GetSNConfig

### Description functions

【Function】Get the SN of the Anviz device

### Request

【Mode】int CChex_GetSNConfig(IntPtr CchexHandle, int DevIdx);

【Parameter】CchexHandle：,successfully create the handle，Input\[Parameter\]句柄，Input\[Parameter\]；

DevIdx,Search the device，Input\[Parameter\]；

### Response

【Return value】1：The command was executed successfully.；Minus：Execute command failure

【Actual Data】CChex_Update, Type return CCHEX_RET_MSGADDNEW_UNICODE_INFO_TYPE Data Part：

uint MachineId; // Device ID

int Result; // 0:OK, -1:error

byte\[16\] sn; // SN，Each number, and it needs to convert into characters

### Sample

int ret = CChex_GetSNConfig(anviz_handle, dev_idx);

### Notice

1.Make sure that CChex_Start has been started successfully before run.

## CChex_DownloadAllRecords

### Description functions

【Function】Download all attendance records.

### Request

【Mode】int CChex_DownloadAllRecords(IntPtr CchexHandle, int DevIdx);

【Parameter】CchexHandle：,successfully create the handle，Input\[Parameter\]；

DevIdx,Search the device，Input\[Parameter\]；

### Response

【Return value】1：The command was executed successfully.；Minus：Execute command failure

【Actual Data】CChex_Update, Type return CCHEX_RET_MSGADDNEW_UNICODE_INFO_TYPE Data Part：

uint MachineId; // Device ID

byte NewRecordFlag; // New or not record?

byte\[5\] EmployeeId; // User ID，

byte\[4\] Date; // date/time，The number of seconds apart from 2000.

byte BackId; // Backup ID，the 3 digits; Card，the 2 digits; Password，the 1~0; 11，7~4 digits; 10 Fingerprints（1~10）digits

byte RecordType // Record type，7 digits; 1：open the door ，0：can't open the door ；3~0 digits attendance status

byte\[3\] WorkType; // Workcode

byte Rsv // Reserved

### Sample

int ret = CChex_DownloadAllRecords(anviz_handle, dev_idx);

### Notice

1.Make sure that CChex_Start has been started successfully before run.

## CChex_DeleteRecordInfo

### Description functions

【Function】Delete all records, or clear all/ some new records flag.

### Request

【Mode】int CChex_DeleteRecordInfo(IntPtr CchexHandle, int DevIdx, ref CCHEX_DEL_RECORD_INFO_STRU Config);

【Parameter】CchexHandle：,successfully create the handle，Input\[Parameter\]；

DevIdx,Search the device，Input\[Parameter\]；

Config,Input\[Parameter\]；

byte del_type; // 0:Delete all records；1：Clear all new record flag；2：Clear some records flag，and the deleted number based on del_count

uint del_count; // del_type =2时，assigned clear new records number

### Response

【Return value】1：The command was executed successfully.；Minus：Execute command failure

【Actual Data】CChex_Update, Type return CCHEX_RET_MSGADDNEW_UNICODE_INFO_TYPE

Data Part：

uint MachineId; // Device ID

int Result; // 0:OK, -1:error

uint deleted_count; // How many records or new records flag be deleted?

### Sample

int ret = CChex_DeleteRecordInfo(anviz_handle, dev_idx, ref delete_record);

### Notice

1.Make sure that CChex_Start has been started successfully before run.

## CChex_GetBasicConfigInfo

### Description functions

【Function】Get the basic configuration of the Anviz device

### Request

【Mode】int CChex_GetBasicConfigInfo(IntPtr CchexHandle, int DevIdx);

【Parameter】CchexHandle：,successfully create the handle，Input\[Parameter\]；

DevIdx,Search the device，Input\[Parameter\]；

### Response

【Return value】1：The command was executed successfully.；Minus：Execute command failure

【Actual Data】CChex_Update, Type return CCHEX_RET_MSGADDNEW_UNICODE_INFO_TYPE Data Part：

uint MachineId; // Device ID

int Result; // 0:OK, -1:error

CCHEX_GET_BASIC_CFG_INFO_STRU Cfg;

byte\[8\] software_version; // firmware version，

uint password; // Password，longest support 6 digits

byte delay_for_sleep; // Delay sleep time，0~250 Mins，0 don't sleep

byte volume; // Volume，0~5，0：mute，5：Max byte language; //Language，0：Simplified Chinese，1：Traditional Chinese，2：English，3.French，4：Spanish，5：Portuguese

byte date_format; // Date format，0:Chinese，1：UK，2：USA byte time_format; // Time format，0：24Houes，1：12Hours

byte machine_status; // Attendance Status，0~15

byte modify_language; // Modify Language，0x10 You can modify the language of the device.

byte cmd_version; // Instruction version

### Sample

int ret = CChex_GetBasicConfigInfo(anviz_handle, dev_idx);

### Notice

- 1.Make sure that CChex_Start has been started successfully before run.；

## CChex_SetBasicConfigInfo

### Description functions

【Function】Set the basic configuration of the Anviz device

### Request

【Mode】int CChex_SetBasicConfigInfo(IntPtr CchexHandle, int DevIdx, ref CCHEX_SET_BASIC_CFG_INFO_STRU Config);

【Parameter】CchexHandle：,successfully create the handle，Input\[Parameter\]；

DevIdx,Search the device，Input\[Parameter\]；

Config,Input\[Parameter\]；Unmodified fields，0xFF

uint password; // Password，String convert into number

byte pwd_len; // Password length

byte delay_for_sleep; // Delay sleep time，0~250 Mins，0 don't sleep

byte volume; // Volume，0~5，0：mute，5：Max

byte language; // Language，0：Simplified Chinese，1：Traditional Chinese，2：English，3.French，4：Spanish，5：Portuguese

byte date_format; // Date format，0:Chinese，1：UK，2：USA

byte time_format; // Time format，0：24Houes，1：12Hours

byte machine_status; // Attendance Status，0~15

byte modify_language; // Modify Language，0x10 You can modify the language of the device.

byte rsv; // Reserved

### Response

【Return value】1：The command was executed successfully.；Minus：Execute command failure

【Actual Data】CChex_Update, Type return CCHEX_RET_MSGADDNEW_UNICODE_INFO_TYPE Data Part：

uint MachineId; // Device ID

int Result; // 0:OK, -1:error

### Sample

int ret = CChex_SetBasicConfigInfo(anviz_handle, dev_idx, ref set_basic_cfg);

### Notice

1.Make sure that CChex_Start has been started successfully before run.

## CChex_ListPersonInfo

### Description functions

【Function】Get all user information.

### Request

【Mode】int CChex_ListPersonInfo(IntPtr CchexHandle, int DevIdx);

【Parameter】CchexHandle：,successfully create the handle，Input\[Parameter\]；

DevIdx,Search the device，Input\[Parameter\]；

### Response

【Return value】1：The command was executed successfully.；Minus：Execute command failure

【Actual Data】CChex_Update, Type return CCHEX_RET_MSGADDNEW_UNICODE_INFO_TYPE

Data Part：

uint MachineId; // Device ID

int CurIdx; // Current index number

int TotalCnt; // The total number of users

byte \[5\]EmployeeId; // User ID，5 bytes,Longest 12digits

byte password_len; // Password length

byte max_password; // Maximum length of password,= 6，unchangeable

int password; // Password

byte max_card_id; // The maximum length of the card number，= 6（3bytes）or 10 （4bytes）, unchangeable

uint card_id; // Card number

byte max_EmployeeName; // The maximum length of a user's name，= 10、20 、64 and160,unchangeable；=160 ,The user name is stored in EmployeeName2.

byte\[64\] EmployeeName; // User name

byte DepartmentId; // Department ID

byte GroupId; // Group ID

byte Mode; // Attendance mode

uint Fp_Status; // Fingerprint register status，0~9:fp; 10:face; 11:iris1; 12:iris2

byte Rserved1; // for 22

byte Rserved2; // for 72 and 22

byte Special; // Special information

// DR info

byte\[160\] EmployeeName2; // Employee Name2

byte\[13\] RFC; // RFC Information

byte\[18\] CURP; // CURP Information

### Sample

int ret = CChex_ListPersonInfo(anviz_handle, dev_idx);

### Notice

1.Make sure that CChex_Start has been started successfully before run.

## CChex_ModifyPersonInfo

### Description functions

【Function】Modify the relevant user's information.

### Request

【Mode】int CChex_ModifyPersonInfo(IntPtr CchexHandle, int DevIdx, ref CCHEX_RET_PERSON_INFO_STRU personlist, byte person_num);

【Parameter】CchexHandle：,successfully create the handle，Input\[Parameter\]；

DevIdx,Search the device，Input\[Parameter\]；

person_num，How many users were changed，Input\[Parameter\]；

Personlist，user details information，Input\[Parameter\]；

uint MachineId; // Device ID

int CurIdx; // Current index number

int TotalCnt; // The total number of users

byte \[5\]EmployeeId; // User ID，5 bytes,Longest 12digits

byte password_len; // Password length

byte max_password; // Maximum length of password,= 6，unchangeable

int password; // Password

byte max_card_id; // The maximum length of the card number，= 6（3bytes）or 10 （4bytes）, unchangeable

uint card_id; // Card number

byte max_EmployeeName; // The maximum length of a user's name，= 10、20 、64 and160,unchangeable；=160 ,The user name is stored in EmployeeName2.

byte\[64\] EmployeeName; // User name

byte DepartmentId; // Department ID

byte GroupId; // Group

byte Mode; // Attendance mode

uint Fp_Status; // Fingerprint register status，0~9:fp; 10:face; 11:iris1; 12:iris2

byte Rserved1; // for 22

byte Rserved2; // for 72 and 22

byte Special; // Special information

// DR info

byte\[160\] EmployeeName2; // User name 2

byte\[13\] RFC; // RFC Information

byte\[18\] CURP; // CURP Information

### Response

【Return value】1：The command was executed successfully.；Minus：Execute command failure

【Actual Data】CChex_Update, Type return CCHEX_RET_MSGADDNEW_UNICODE_INFO_TYPE

Data Part：

uint MachineId; // Device ID

int Result; // 0:OK, -1:error

### Sample

int ret = CChex_ModifyPersonInfo(anviz_handle, dev_idx, ref item, 1);

### Notice

1.Make sure that CChex_Start has been started successfully before run；

## CChex_DeletePersonInfo

### Description functions

【Function】Deletes the assigned person's information.

### Request

【Mode】int CChex_DeletePersonInfo(IntPtr CchexHandle, int DevIdx, ref CCHEX_DEL_PERSON_INFO_STRU Config);

【Parameter】CchexHandle：,successfully create the handle，Input\[Parameter\]；

DevIdx,Search the device，Input\[Parameter\]；

Config,Input\[Parameter\]；

byte \[\]EmployeeId; // User ID

byte operation; // Which information be deleted ? The 3 digits: Card; The 2 digits: Password; The 1~0 digits: 1~0 digits ：11; 7~4 digits：10 Fingerprints（1~10）；0xFF: Delete all user's information .

### Response

【Return value】1：The command was executed successfully.；Minus：Execute command failure

【Actual Data】CChex_Update, Type return CCHEX_RET_MSGADDNEW_UNICODE_INFO_TYPE

Data Part：

uint MachineId; // Device ID

int Result; // 0:OK, -1:error

### Sample

int ret = CChex_DeletePersonInfo(anviz_handle, dev_idx, ref delete_item);

### Notice

1.Make sure that CChex_Start has been started successfully before run.

## CChex_DownloadFingerPrint

### Description functions

【Function】Download fingerprint template from Anviz device

### Request

【Mode】int CChex_DownloadFingerPrint(IntPtr CchexHandle, int DevIdx, byte\[\] EmployeeId, byte FingerIdx);

【Parameter】CchexHandle：,successfully create the handle，Input\[Parameter\]；

DevIdx,Search the device，Input\[Parameter\]；

EmployeeId，User ID, Input\[Parameter\]；

FingerIdx，Fingerprint Indexing，1~10：means fingerprint number 1~10，Input\[Parameter\]；

### Response

【Return value】1：The command was executed successfully.；Minus：Execute command failure

【Actual Data】CChex_Update, Type return CCHEX_RET_MSGADDNEW_UNICODE_INFO_TYPE

Data Part：

uint MachineId; // Device ID

int Result; // 0:OK, -1:error

byte \[\]EmployeeId; // User ID

byte FpIdx; // Fingerprint indexing，1~10：means fingerprint number 1~10

uint fp_data_len; // The length of the fingerprint data.

byte \[\]Data; // fingerprint data

### Sample

int ret = CChex_DownloadFingerPrint(anviz_handle, dev_idx, EmployeeID, FpIdx);

### Notice

1.Make sure that CChex_Start has been started successfully before run.

## CChex_UploadFingerPrint

### Description functions

【Function】Upload fingerprint template to Anviz device

### Request

【Mode】int CChex_UploadFingerPrint(IntPtr CchexHandle, int DevIdx, byte\[\] EmployeeId, byte FingerIdx, byte\[\] FingerData, int DataLen);

【Parameter】CchexHandle：,successfully create the handle，Input\[Parameter\]；

DevIdx,Search the device，Input\[Parameter\]；

EmployeeId，User ID，Input\[Parameter\]；

FingerIdx， Fingerprint indexing，1~10：means fingerprint number 1~10，Input\[Parameter\]；

FingerData，fingerprint data，Input\[Parameter\]；

DataLen，The length of the fingerprint data，Input\[Parameter\]；

### Response

【Return value】1：The command was executed successfully.；Minus：Execute command failure

【Actual Data】CChex_Update, Type return CCHEX_RET_MSGADDNEW_UNICODE_INFO_TYPE

Data Part：

uint MachineId; // Device ID

int Result; // 0:OK, -1:error

### Sample

int ret = CChex_DownloadFingerPrint(anviz_handle, dev_idx, EmployeeID, FpIdx);

### Notice

- 1.Make sure that CChex_Start has been started successfully before run.

## CChex_GetTime

### Description functions

【Function】Get the date/time of the Anviz device.

### Request

【Mode】int CChex_GetTime(IntPtr CchexHandle, int DevIdx);

【Parameter】CchexHandle：,successfully create the handle，Input\[Parameter\]；

DevIdx,Search the device，Input\[Parameter\]；

### Response

【Return value】1：The command was executed successfully.；Minus：Execute command failure

【Actual Data】CChex_Update, Type return CCHEX_RET_MSGADDNEW_UNICODE_INFO_TYPE

Data Part：

uint MachineId; // Device ID

int Result; // 0:OK, -1:error

CCHEX_MSG_GETTIME_STRU Cfg;

uint Year; //Year

uint Month; //Month

uint Day; //Day

uint Hour; //Hour

uint Min; //Minute

uint Sec; //Second

### Sample

int ret = CChex_GetTime(anviz_handle, dev_idx);

### Notice

1.Make sure that CChex_Start has been started successfully before run.

## CChex_InitUserArea

### Description functions

【Function】Initial user area

Initialize all user data areas，Clear all user data, fingerprint data, password/card data.

### Request

【Mode】int CChex_InitUserArea(IntPtr CchexHandle, int DevIdx);

【Parameter】CchexHandle：,successfully create the handle，Input\[Parameter\]；

DevIdx,Search the device，Input\[Parameter\]；

### Response

【Return value】1：The command was executed successfully.；Minus：Execute command failure

【Actual Data】CChex_Update, Type return CCHEX_RET_MSGADDNEW_UNICODE_INFO_TYPE

Data Part：

uint MachineId; // Device ID

int Result; // 0:OK, -1:error

### Sample

int ret = CChex_InitUserArea(anviz_handle, dev_idx);

### Notice

1.Make sure that CChex_Start has been started successfully before run.

## CChex_InitSystem

### Description functions

【Function】initial device

initialize the Anviz device to restore factory Default.

### Request

【Mode】int CChex_InitSystem(IntPtr CchexHandle, int DevIdx);

【Parameter】CchexHandle：,successfully create the handle，Input\[Parameter\]；

DevIdx,Search the device，Input\[Parameter\]；

### Response

【Return value】1：The command was executed successfully.；Minus：Execute command failure

【Actual Data】CChex_Update, Type return CCHEX_RET_MSGADDNEW_UNICODE_INFO_TYPE

Data Part：

uint MachineId; // Device ID

int Result; // 0:OK, -1:error

### Sample

int ret = CChex_InitSystem(anviz_handle, dev_idx);

### Notice

- 1.Make sure that CChex_Start has been started successfully before run.

## CChex_GetBasicConfigInfo2

### Description functions

【Function】Get the Anviz device's configuration2

Get device's identification precision, fixed wiegand head, wiegand mode, workcode, real-time attendance records , schedule bell , lock control delay, record overflow warning, repeated attendance record time , door sensor delay, schedule bell delay.

### Request

【Mode】int CChex_GetBasicConfigInfo2(IntPtr CchexHandle, int DevIdx);

【Parameter】CchexHandle：,successfully create the handle，Input\[Parameter\]；

DevIdx,Search the device，Input\[Parameter\]；

### Response

【Return value】1：The command was executed successfully.；Minus：Execute command failure

【Actual Data】CChex_Update, Type return CCHEX_RET_MSGADDNEW_UNICODE_INFO_TYPE

Data Part：

uint MachineId; // Device ID

int Result; // 0:OK, -1:error

CCHEX_GET_BASIC_CFG_INFO2_STRU_EXT_INF Param;

byte compare_level; //identification precision

byte wiegand_range; //fixed wiegand head

byte wiegand_type; //wiegand mode

byte work_code; //Workcode function, enable or disable?

byte real_time_send; //Realtime function ,enable or disable?

byte auto_update; //Auto-update function, enable or disable?

byte bell_lock; //Schedule bell function, enable or disable?

byte lock_delay; //Lock time delay

uint record_over_alarm; //Record overflow alarm

byte re_attendance_delay; //Repeat attendance time delay

byte door_sensor_alarm; //Door sensor time delay

byte bell_delay; //Schedule bell time delay

byte correct_time; //Time calibration

### Sample

int ret = CChex_GetBasicConfigInfo2(anviz_handle, dev_idx);

### Notice

- 1.Make sure that CChex_Start has been started successfully before run.

## CChex_SetBasicConfigInfo2

### Description functions

【Function】Set up the Anviz device's configuration information2

### Request

【Mode】int CChex_SetBasicConfigInfo2(IntPtr CchexHandle, int DevIdx, ref CCHEX_GET_BASIC_CFG_INFO2_STRU_EXT_INF config);

【Parameter】CchexHandle：,successfully create the handle，Input\[Parameter\]；

DevIdx,Search the device，Input\[Parameter\]；

config: Input\[Parameter\]；

byte compare_level; //identification precision

byte wiegand_range; //fixed wiegand head

byte wiegand_type; //wiegand mode

byte work_code; //Workcode function, enable or disable?

byte real_time_send; //Realtime function ,enable or disable?

byte auto_update; //Auto-update function, enable or disable?

byte bell_lock; //Schedule bell function, enable or disable?

byte lock_delay; //Lock time delay

uint record_over_alarm; //Record overflow alarm

byte re_attendance_delay; //Repeat attendance time delay

byte door_sensor_alarm; //Door sensor time delay

byte bell_delay; //Schedule bell time delay

byte correct_time; //Time calibration

### Response

【Return value】1：The command was executed successfully.；Minus：Execute command failure

【Actual Data】CChex_Update, Type return CCHEX_RET_MSGADDNEW_UNICODE_INFO_TYPE

Data Part：

uint MachineId; // Device ID

int Result; // 0:OK, -1:error

### Sample

int ret = CChex_SetBasicConfigInfo2(anviz_handle, dev_idx,ref config);

### Notice

- 1.Make sure that CChex_Start has been started successfully before run.

## CChex_GetPeriodTime

### Description functions

【Function】Get time zone from device

Read the timezone information，Total 32 timezones。

### Request

【Mode】int CChex_GetPeriodTime(IntPtr CchexHandle, int DevIdx, byte SerialNumbe);

【Parameter】CchexHandle：,successfully create the handle，Input\[Parameter\]；

DevIdx,Search the device，Input\[Parameter\]；

SerialNumbe,timezone number，Input\[Parameter\]；

### Response

【Return value】1：The command was executed successfully.；Minus：Execute command failure

【Actual Data】CChex_Update, Type return CCHEX_RET_MSGADDNEW_UNICODE_INFO_TYPE

Data Part：

uint MachineId; // Device ID

int Result; // 0:OK, -1:error

CCHEX_GET_PERIOD_TIME_ONE_STRU_EXT_INF day\[7\]; //One week 7 days,28Byte

byte StartHour; //Start hour

byte StartMin; //Start mins

byte EndHour; //End hour

byte EndMin; //End mins

### Sample

int ret = CChex_GetPeriodTime(anviz_handle, dev_idx,SerialNumbe);

### Notice

- 1.Make sure that CChex_Start has been started successfully before run.

## CChex_SetPeriodTime

### Description functions

【Function】Set timezone

Total 32 timezones。

### Request

【Mode】int CChex_SetPeriodTime(IntPtr CchexHandle, int DevIdx, ref CCHEX_SET_PERIOD_TIME_STRU_EXT_INF config);

【Parameter】CchexHandle：,successfully create the handle，Input\[Parameter\]；

DevIdx,Search the device，Input\[Parameter\]；

Config: Input\[Parameter\]；

byte SerialNumbe;

CCHEX_GET_PERIOD_TIME_ONE_STRU_EXT_INF day\[7\]; //one week with 7 days,so should be have 7 groups,28Byte

byte StartHour; //Start Hour

byte StartMin; //Start Mins

byte EndHour; //End Hours

byte EndMin; //End Mins

### Response

【Return value】1：The command was executed successfully.；Minus：Execute command failure

【Actual Data】CChex_Update, Type return CCHEX_RET_MSGADDNEW_UNICODE_INFO_TYPE

Data Part：

uint MachineId; // Device ID

int Result; // 0:OK, -1:error

### Sample

int ret = CChex_SetPeriodTime(anviz_handle, dev_idx,ref config);

### Notice

- 1.Make sure that CChex_Start has been started successfully before run；

## CChex_GetTeamInfo

### Description functions

【Function】Get Group information

Read one group information, group number 0-16, group0 = normal close; group1=normal open. Only read group2 - group16.

### Request

【Mode】int CChex_GetTeamInfo(IntPtr CchexHandle, int DevIdx, byte TeamNumbe);

【Parameter】CchexHandle：,successfully create the handle，Input\[Parameter\]；

DevIdx:Search the device，Input\[Parameter\]；

TeamNumbe:Group number,Input\[Parameter\]；

### Response

【Return value】1：The command was executed successfully.；Minus：Execute command failure

【Actual Data】CChex_Update, Type return CCHEX_RET_MSGADDNEW_UNICODE_INFO_TYPE

Data Part：

uint MachineId; // Device ID

int Result; // 0:OK, -1:error

byte\[4\] PeriodTimeNumber;//4Group timezone number(refer to 2.29)

### Sample

int ret = CChex_GetTeamInfo(anviz_handle, dev_idx,TeamNumbe);

### Notice

- 1.Make sure that CChex_Start has been started successfully before run.

## CChex_SetTeamInfo

### Description functions

【Function】Setup group

Setup one group, group number 0- 16, group 0= normal close; group1= normal open, only setup group2 -group16.

### Request

【Mode】int CChex_SetTeamInfo(IntPtr CchexHandle, int DevIdx, ref CCHEX_SET_TEAM_INFO_STRU_EXT_INF config);

【Parameter】CchexHandle：,successfully create the handle，Input\[Parameter\]；

DevIdx,Search the device，Input\[Parameter\]；

Config: Input\[Parameter\]；

byte TeamNumbe; //Group Number

byte\[4\] PeriodTimeNumber; //4Group ,timezone

### Response

【Return value】1：The command was executed successfully.；Minus：Execute command failure

【Actual Data】CChex_Update, Type return CCHEX_RET_MSGADDNEW_UNICODE_INFO_TYPE

Data Part：

uint MachineId; // Device ID

int Result; // 0:OK, -1:error

### Sample

int ret = CChex_SetTeamInfo(anviz_handle, dev_idx,ref config);

### Notice

- 1.Make sure that CChex_Start has been started successfully before run.

## CCHex_AddFingerprintOnline

### Description functions

【Function】Register fingerprint online

### Request

【Mode】int CCHex_AddFingerprintOnline(IntPtr CchexHandle, int DevIdx, ref CCHEX_ADD_FINGERPRINT_ONLINE_STRU_EXT_INF Param);

【Parameter】CchexHandle：,successfully create the handle，Input\[Parameter\]；

DevIdx,Search the device，Input\[Parameter\]；

Param: Input\[Parameter\]；

byte\[5\] EmployeeId; //User ID

byte BackupNum; //Backup number

### Response

【Return value】1：The command was executed successfully.；Minus：Execute command failure

【Actual Data】CChex_Update, Type return CCHEX_RET_MSGADDNEW_UNICODE_INFO_TYPE

Data Part：

uint MachineId; // Device ID

int Result; // 0:OK, -1:error

### Sample

int ret = CCHex_AddFingerprintOnline(anviz_handle, dev_idx,ref Param);

### Notice

- Make sure that CChex_Start has been started successfully before run.
- After the recall, the device will register the fingerprint twice, timeout is 10 seconds, the registration error (timeout, operation failure, the user already exists, the fingerprint exists) returned -1, and successfully returns 0.

## CCHex_ForcedUnlock

### Description functions

【Function】Open the door by software (Force open the lock)

### Request

【Mode】int CCHex_ForcedUnlock(IntPtr CchexHandle, int DevIdx, ref CCHEX_FORCED_UNLOCK_STRU_EXT_INF Param);

【Parameter】CchexHandle：,successfully create the handle，Input\[Parameter\]；

DevIdx,Search the device，Input\[Parameter\]；

Param: Input\[Parameter\]；( Param =IntPtr.Zerohe ; default null pointer , Malaysia panasonic project customization needs to enter Param);

byte LockCmd; //Lock command

byte\[5\] EmployeeId; //User ID

### Response

【Return value】1：The command was executed successfully.；Minus：Execute command failure

【Actual Data】CChex_Update, Type return CCHEX_RET_MSGADDNEW_UNICODE_INFO_TYPE

Data Part：

uint MachineId; // Device ID

int Result; // 0:OK, -1:error

### Sample

int ret = CCHex_ForcedUnlock(anviz_handle, DevIdx,IntPtr.Zero);

### Notice

- Make sure that CChex_Start has been started successfully before run.；
- If Not Malaysia Panasonic project customization, Param none pointer..

## CCHex_Udp_Search_Dev

### Description functions

【Function】UDP Research the Anviz device

### Request

【Mode】int CCHex_Udp_Search_Dev(IntPtr CchexHandle);

【Parameter】CchexHandle：,successfully create the handle，Input\[Parameter\]；

### Response

【Return value】1：The command was executed successfully.；Minus：Execute command failure

【Actual Data】CChex_Update, Type return CCHEX_RET_MSGADDNEW_UNICODE_INFO_TYPE

Data Part：(Data length =sizeof(int)+DevNum\*sizeof(CCHEX_UDP_SEARCH_STRU_EXT_INF))

int DevNum; // quantity of device, ( =0, No device,data length =4)

CCHEX_UDP_SEARCH_STRU_EXT_INF\[\] dev_net_info ; //Device Data(Single 180Byte)

uint MachineId; //Device ID

int Result; //0: ok -1: fail

int DevHardwareType; //device type(0:Normal device;1:support DNS;2:support wifi)

byte\[167\] Data; //Data(Convert based on device type)

Byte Padding; //Data alignment, padding

\~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

byte\[167\] Data: Data type definition

Device type = 0 :Normal device

CCHEX_UDP_SEARCH_STRU BasicSearchInfo; //normal device 63Byte

byte\[10\] DevType; //device type

byte\[16\] DevSerialNum; //device serial numebr

byte\[4\] IpAddr; //IP Address

byte\[4\] IpMask; //MASK

byte\[4\] GwAddr; //Gateway

byte\[6\] MacAddr; //MAC

byte\[4\] ServAddr; //Server IP

byte\[2\] Port; //Port

byte NetMode; //network mode

byte\[8\] Version; //Firmware version

byte\[4\] Reserved; //Reserved

Device type = 1 :Support DNS firmware

CCHEX_UDP_SEARCH_WITH_DNS_STRU DnsSearchInfo; //Support DNS firmware 167Byte

CCHEX_UDP_SEARCH_STRU BasicSearchInfo; //device type 0:normal device 63Byte structure

byte\[4\] Dns; //DNS

byte\[100\] Url; //URL

Device type = 2 : Support wifi

CCHEX_UDP_SEARCH_TWO_CARD_STRU TwocardSearchInfo; \\\\ support wifi 102Byte

byte\[10\] DevType; //Device type

byte\[16\] DevSerialNum; //Device serial number

byte\[4\] ServAddr; //Server IP

byte\[2\] Port; //Port

byte NetMode; //Network mode

byte\[8\] Version; //Firmware version

byte\[4\] Reserved; //Reserved

byte CardNumber; //Network card number

CCHEX_UDP_SEARCH_CARD_STRU CardInfo\[2\]; //2group network card information: single network card information = 28Byte

byte\[10\] CardName; //Network card name

byte\[4\] IpAddr; //IP Address

byte\[4\] IpMask; //MASK

byte\[4\] GwAddr; //Gateway

byte\[6\] MacAddr; //MAC Address

\~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

### Sample

int ret = CCHex_Udp_Search_Dev(anviz_handle);

### Notice

- Make sure that CChex_Start has been started successfully before run.

## CCHex_Udp_Set_Dev_Config

### Description functions

【Function】UDP setting device configuration

### Request

【Mode】int CCHex_Udp_Set_Dev_Config(IntPtr CchexHandle, ref CCHEX_UDP_SET_DEV_CONFIG_STRU_EXT_INF config);

【Parameter】CchexHandle：,successfully create the handle，Input\[Parameter\]；

Config: Input\[Parameter\]；

byte\[4\] IpAddr; //IP Address

byte\[4\] IpMask; //MASK

byte\[4\] GwAddr; //Gateway

byte\[6\] MacAddr; //MAC address

byte\[4\] ServAddr; //Server IP

byte\[2\] Port; //Port

byte NetMode; //Network mode

byte\[3\] Padding; //structure alignment,padding 3byte

uint NewMachineId; //New Device ID

byte\[4\] Reserved; //Reserved

byte\[12\] DevUserName; //User name

byte\[12\] DevPassWord; //Password

int DevHardwareType; //Device type 0:Normal device ;1:support DNS device

byte\[4\] Dns; //DNS device type = 0 Dns , unchangeable

byte\[100\] Url; //URL device type = 0 Url,unchangeable

### Response

【Return value】1：The command was executed successfully.；Minus：Execute command failure

【Actual Data】CChex_Update, Type return CCHEX_RET_MSGADDNEW_UNICODE_INFO_TYPE

Data Part：

uint MachineId; // Device ID

int Result; // 0:OK, -1:error

### Sample

int ret = CCHex_Udp_Set_Dev_Config(anviz_handle,ref config);

### Notice

- 1.Make sure that CChex_Start has been started successfully before run.

## CCHex_ClientConnect

### Description functions

【Function】The client actively connects to the server.

### Request

【Mode】int CCHex_ClientConnect(IntPtr CchexHandle, byte\[\] Ip, int Port);

【Parameter】CchexHandle：successfully create the handle，Input\[Parameter\]；

Ip: Server IP address，Input\[Parameter\]；

Port: Server Port, Input\[Parameter\]；

### Response

【Return value】1：The command was executed successfully.；Minus：Execute command failure

【Actual Data】Failure:CChex_Update,Type return CCHEX_RET_CLINECT_CONNECT_TYPE;

Data Part：

int Result; //-1 err

byte\[24\] Addr; //IP:Port 如(192.168.100.100:5010)

Successful:CChex_Update, Type return CCHEX_RET_MSGADDNEW_UNICODE_INFO_TYPE

Data Part：

int DevIdx; //

uint MachineId; //Device ID

byte\[24\] Addr; //IP:Port For example(192.168.100.100:5010)

byte\[8\] Version; //Version Number

byte\[8\] DevType; //Device type

int DevTypeFlag; //Type flag

### Sample

int ret = CCHex_ClientConnect(anviz_handle,Ip,Port);

### Notice

- 1.Make sure that CChex_Start has been started successfully before run.

## CCHex_ClientDisconnect

### Description functions

【Function】The client actively disconnects the server.

### Request

【Mode】int CCHex_ClientDisconnect(IntPtr CchexHandle, int DevIdx);

【Parameter】CchexHandle：,successfully create the handle，Input\[Parameter\]；

DevIdx,Search the device，Input\[Parameter\]；

### Response

【Return value】1：The command was executed successfully.；Minus：Execute command failure

【Actual Data】Failure:CChex_Update, Type return CCHEX_RET_MSGADDNEW_UNICODE_INFO_TYPE

Data Part：

int Result; // -1:error

int DevIdx; //

Successful:CChex_Update, Type return CCHEX_RET_MSGADDNEW_UNICODE_INFO_TYPE

Data Part：

int DevIdx; //考勤机的设备索引

uint MachineId; //Device ID

uint Live; //

byte\[24\] Addr; //ip:Port 如(192.168.100.100:5010)

byte\[8\] Version; //Version number

byte\[8\] DevType; //Device type

### Sample

int ret = CCHex_ClientDisconnect(anviz_handle,DevIdx);

### Notice

1.Make sure that CChex_Start has been started successfully before run.

# Return value

1：Successful execution；

2：parameter error；

3：equipment resources Insufficient ；

4：Unknown error；

##

## 2.39 CChex_GetInfomationCode

### 2.39.1 Description function

【Function】Gets the factory information

Read the factory information ANSI version data:10Byte,UNICODE version data:20Byte

### 2.39.2 Request

【Mode】int CChex_GetInfomationCode(IntPtr CchexHandle, int DevIdx);

【Parameter】CchexHandle：,successfully create the handle，Input\[Parameter\]；

DevIdx: Search the device，Input\[Parameter\]

### 2.39.3 Response

【Return value】1：The command was executed successfully.；Minus：Execute command failure

【Actual Data】CChex_Update, Type return CCHEX_RET_GET_INFOMATION_CODE_TYPE

Data Part：

uint MachineId; // Device ID

int Result; // 0:OK, -1:error

int fp_len; //ANSI VERSION:fp_len == 10 UNICODE VERSION:fp_len == 20

byte\[20\] data; //

### 2.39.4 Sample

int ret = CChex_GetInfomationCode(anviz_handle, dev_idx);

### 2.39.5 Notice

- Make sure that CChex_Start has been started successfully before run.；

## 2.40 CChex_SetInfomationCode

### 2.40.1 Description function

【Function】Modify the factory information

Modify the factory information ANSI version data:10Byte ,UNICODE version data:20Byte

### 2.40.2 Request

【Mode】int CChex_SetInfomationCode(IntPtr CchexHandle, int DevIdx ,byte\[\] Data, int DataLen);

【Parameter】CchexHandle：,successfully create the handle，Input\[Parameter\]；

DevIdx: Search the device，Input\[Parameter\]

Data: Byte\[10\] or Byte\[20\] parameter，Input\[Parameter\]；

DataLen: Write the parameters based on the version, , ANSI version = 10,UNICODE version = 20, Input\[Parameter\]；

### 2.40.3Response

【Return value】1：The command was executed successfully.；Minus：Execute command failure

【Actual Data】CChex_Update, Type return CCHEX_RET_SET_INFOMATION_CODE_TYPE

Data Part：

uint MachineId; // Device ID

int Result; // 0:OK, -1:error

### 2.40.4 Sample

int ret = CChex_SetInfomationCode(anviz_handle, dev_idx,Data,DataLen);

### 2.40.5 Notice

- Make sure that CChex_Start has been started successfully before run.；

## 2.41 CChex_GetBellInfo

### 2.41.1 Description function

【Function】Get the Schedule bell information

Read all time of schedule bell，totally 30.

### 2.41.2 Request

【Mode】int CChex_GetBellInfo(IntPtr CchexHandle, int DevIdx);

【Parameter】CchexHandle：,successfully create the handle，Input\[Parameter\]；

DevIdx: Search the device，Input\[Parameter\]

### 2.41.3 Response

【Return value】1：The command was executed successfully.；Minus：Execute command failure

【Actual Data】CChex_Update, Type return CCHEX_RET_GET_BELL_INFO_TYPE

Data Part：

uint MachineId; // Device ID

int Result; // 0:OK, -1:error

CCHEX_RET_GET_BELL_TIME_POINT time_point\[30\];

byte hour;

byte minute;

byte flag_week; //Week mark/flag(Binary 0000000 means：Sat Fri Thu Wed Tue Mon Sun)

Byte\[2\] padding; //data structure alignment , invalid data

### 2.41.4 Sample

int ret = CChex_GetBellInfo(anviz_handle, dev_idx);

### 2.41.5 Notice

- Make sure that CChex_Start has been started successfully before run.；

## 2.42 CChex_SetBellInfo

### 2.42.1 Description function

【Function】Setup the schedule bell information

Setup the time of schedule bell

### 2.42.2 Request

【Mode】int CChex_SetBellInfo(IntPtr CchexHandle, int DevIdx ,byte BellTimeNum,byte Hour,byte Min,byte FlagWeek);

【Parameter】CchexHandle：,successfully create the handle，Input\[Parameter\]；

DevIdx: Search the device，Input\[Parameter\]

BellTimeNum：The time to ring the bell，Input\[Parameter\]；

Hour：Hour，Input\[Parameter\]；

Min：Minute，Input\[Parameter\]；

FlagWeek：Week mark/flag(Binary 0000000means：Sat Fri Thu Wed Tue Mon Sun)，Input\[Parameter\]；

### 2.42.3 Response

【Return value】1：The command was executed successfully.；Minus：Execute command failure

【Actual Data】CChex_Update, Type return CCHEX_RET_SET_BELL_INFO_TYPE

Data Part：

uint MachineId; // Device ID

int Result; // 0:OK, -1:error

### 2.42.4 Sample

int ret = CChex_SetBellInfo(CchexHandle,DevIdx ,BellTimeNum,Hour,Min,FlagWeek);

### 2.42.5 Notice

- Make sure that CChex_Start has been started successfully before run.；

## 2.43 CChex_GetUserAttendanceStatusInfo

### 2.43.1 Description function

【Function】Gets the self-defining attendance status table

Read the information of self-defining attendance status.

### 2.43.2 Request

【Mode】int CChex_GetUserAttendanceStatusInfo(IntPtr CchexHandle,int DevIdx);

【Parameter】CchexHandle：,successfully create the handle，Input\[Parameter\]；

DevIdx: Search the device，Input\[Parameter\]

### 2.43.3 Response

【Return value】1：The command was executed successfully.；Minus：Execute command failure

【Actual Data】CChex_Update, Type return CCHEX_RET_GET_USER_ATTENDANCE_STATUS_TYPE

Data Part：

uint MachineId; // Device ID

int Result; // 0:OK, -1:error

int fp_len; //ANSI VERSION：fp_len == 80 UNICODE VERSION fp_len == 160

Byte atten_status_number; //Number of attendance status == 8 default

Byte\[160\] data_info; //data format: ANSI VERSION: unsigned char \[8\]\[10\] UNICODE VERSION: unsigned char\[8\]\[20\] 。

Byte\[3\] padding; //data structure alignment, invalid data

### 2.43.4 Sample

int ret = CChex_GetUserAttendanceStatusInfo(anviz_handle, dev_idx);

### 2.43.5 Notice

- Make sure that CChex_Start has been started successfully before run.；

## 2.44 CChex_SetUserAttendanceStatusInfo

### 2.44.1 Description function

【Function】Setup the self-defining attendance status

Setup the self-defining attendance status.

### 2.44.2 Request

【Mode】int CChex_SetUserAttendanceStatusInfo(IntPtr CchexHandle, int DevIdx,ref CCHEX_SET_USER_ATTENDANCE_STATUS_STRU Param);

【Parameter】CchexHandle：,successfully create the handle，Input\[Parameter\]；

DevIdx: Search the device，Input\[Parameter\]

Param：8 groups self-defining status,Input\[Parameter\]；

uint fp_len;//ANSI VERSION fp_len = 80 UNICODE VERSION fp_len = 160

Byte atten_status_number; //Number of attendance status 8 default

Byte\[160\] data_info;//ANSI VERSION:8\]\[10\]，UNICODE VERSION:\[8\]\[20\]

Byte\[3\] padding; //alignment, invalid data

### 2.44.3 Response

【Return value】1：The command was executed successfully.；Minus：Execute command failure

【Actual Data】CChex_Update, Type return CCHEX_RET_SET_USER_ATTENDANCE_STATUS_TYPE

Data Part：

uint MachineId; // Device ID

int Result; // 0:OK, -1:error

### 2.44.4 Sample

int ret = CChex_SetUserAttendanceStatusInfo(CchexHandle,DevIdx ,ref Param);

### 2.44.5 Notice

- Make sure that CChex_Start has been started successfully before run.；

## 2.45 CChex_ClearAdministratFlag

### 2.45.1 Description function

【Function】Clear the administrator flag

Clear all administrator flag.

### 2.45.2 Request

【Mode】int CChex_ClearAdministratFlag(IntPtr CchexHandle,int DevIdx);

【Parameter】CchexHandle：,successfully create the handle，Input\[Parameter\]；

DevIdx: Search the device，Input\[Parameter\]

### 2.45.3 Response

【Return value】1：The command was executed successfully.；Minus：Execute command failure

【Actual Data】CChex_Update, Type return CCHEX_RET_CLEAR_ADMINISTRAT_FLAG_TYPE

Data Part：

uint MachineId; // Device ID

int Result; // 0:OK, -1:error

### 2.45.4 Sample

int ret = CChex_ClearAdministratFlag(anviz_handle, dev_idx);

### 2.45.5 Notice

- Make sure that CChex_Start has been started successfully before run.；

## 2.46 CChex_GetSpecialStatus

### 2.46.1 Description function

【Function】Get the special status

Get the current special status. Only for VF30/VP30/T60+

### 2.46.2 Request

【Mode】int CChex_GetSpecialStatus(IntPtr CchexHandle,int DevIdx);

【Parameter】CchexHandle：,successfully create the handle，Input\[Parameter\]；

DevIdx: Search the device，Input\[Parameter\]

### 2.46.3 Response

【Return value】1：The command was executed successfully.；Minus：Execute command failure

【Actual Data】CChex_Update, Type return CCHEX_RET_GET_SPECIAL_STATUS_TYPE

Data Part：

uint MachineId; // Device ID

int Result; // 0:OK, -1:error

Byte status; //Byte1：Door alarm status 0-normal ,status 1-alarm status; Byte5：Door status 0-close , 1-open; Byte6：Door sensor status 0-close, 1-open; Byte7：Lock status 0-close, 1-open

Byte\[7\] reserved; //reserved useless in the temporary

### 2.46.4 Sample

int ret = CChex_GetSpecialStatus(anviz_handle, dev_idx);

### 2.46.5 Notice

- Make sure that CChex_Start has been started successfully before run.；

## 2.47 CChex_GetAdminCardnumberPassword

### 2.47.1 Description function

【Function】Read administrator card number or password. Only for T5/M5/T50m

Get administrator card number or password.

### 2.47.2 Request

【Mode】int CChex_GetAdminCardnumberPassword(IntPtr CchexHandle,int DevIdx);

【Parameter】CchexHandle：,successfully create the handle，Input\[Parameter\]；

DevIdx: Search the device，Input\[Parameter\]

### 2.47.3 Response

【Return value】1：The command was executed successfully.；Minus：Execute command failure

【Actual Data】CChex_Update, Type return CCHEX_RET_GET_ADMIN_CARD_PWD_TYPE

Data Part：

uint MachineId; // Device ID

int Result; // 0:OK, -1:error

Byte\[13\] data; //device type : T5A,T50

\* Parameter :data\[13\] : if the device type is T5A，so:

DATA Add card ;delete card ;stress/duress card; special information Byte 1-4 5-8 9-12 13

The special information define as below:

Byte 0：Add card

Byte 1：Delete card

Byte 2：Stress/duress

If the device type is T50，so:

DATA Password length+ passwod Reserved

Byte 1-3 4-13

Password length = Byte(1) >> 4

Byte\[3\] padding; //data structure alignment, invalid data

### 2.47.4 Sample

int ret = CChex_GetAdminCardnumberPassword(anviz_handle, dev_idx);

### 2.47.5 Notice

- Make sure that CChex_Start has been started successfully before run.；

## 2.48 CChex_SetAdminCardnumberPassword

### 2.48.1 Description function

【Function】Setup administrator card or password. Only for T5/M5/T50M

Setup T5A/M5A admin card and T50M admin password.

### 2.48.2 Request

【Mode】int CChex_SetAdminCardnumberPassword(IntPtr CchexHandle, int DevIdx,byte\[\] Data, int DataLen);

【Parameter】CchexHandle：,successfully create the handle，Input\[Parameter\]；

DevIdx: Search the device，Input\[Parameter\]

Data\[13\]：Parameter refer to 2.47 :data\[13\] description ，Input\[Parameter\]；

DataLen： length = 13，Input\[Parameter\]；

### 2.48.3 Response

【Return value】1：The command was executed successfully.；Minus：Execute command failure

【Actual Data】CChex_Update, Type return CCHEX_RET_SET_ADMIN_CARD_PWD_TYPE

Data Part：

uint MachineId; // Device ID

int Result; // 0:OK, -1:error

### 2.48.4 Sample

int ret = CChex_SetAdminCardnumberPassword(CchexHandle,DevIdx,Data,DataLen);

### 2.48.5 Notice

- Make sure that CChex_Start has been started successfully before run.；

## 2.49 CChex_GetDSTParam

### 2.49.1 Description function

【Function】Read the daylight saving time parameters

Read the flag of daylight saving time and time zone .

### 2.49.2Request

【Mode】int CChex_GetDSTParam(IntPtr CchexHandle,int DevIdx);

【Parameter】CchexHandle：,successfully create the handle，Input\[Parameter\]；

DevIdx: Search the device，Input\[Parameter\]

### 2.49.3 Response

【Return value】1：The command was executed successfully.；Minus：Execute command failure

【Actual Data】CChex_Update, Type return CCHEX_RET_GET_DST_PARAM_TYPE

Data Part：

uint MachineId; // Device ID

int Result; // 0:OK, -1:error

CCCHEX_SET_DST_PARAM_STRU param; //16byte

byte enabled; //0-Disable 1-enable；

byte ate_week_type; //1-Date format 2-week format；

GET_DST_PARAM_TIME start_time; //7 byte

byte month;

byte day;

byte week_num; //Weekly definition：0x01-0x04： First 1-4 week 0x81-0x82：Last1-2 week

byte flag_week; //Week flag: flag_week 0-6:(Binary 0000000 means：Sat Fri Thu Wed Tue Mon Sun)

byte hour;

byte minute;

byte sec;

GET_DST_PARAM_TIME special_time； //7 byte

### 2.49.4 Sample

int ret = CChex_GetDSTParam(anviz_handle, dev_idx);

### 2.49.5 Notice

- Make sure that CChex_Start has been started successfully before run.；

## 2.50 CChex_SetDSTParam

### 2.50.1 Description function

【Function】Setup daylight saving time

Setup daylight saving time flag and time zone

### 2.50.2 Request

【Mode】int CChex_SetDSTParam(IntPtr CchexHandle, int DevIdx,ref CCHEX_SET_DST_PARAM_STRU Param);

【Parameter】CchexHandle：,successfully create the handle，Input\[Parameter\]；

DevIdx: Search the device，Input\[Parameter\]

Param：16byte，Input\[Parameter\]；

byte enabled; //0-disable 1-enable；

byte ate_week_type; //1-Date format 2-week format；

GET_DST_PARAM_TIME start_time; //7 byte

byte month;

byte day;

byte week_num; //Weekly definition：0x01-0x04： First 1-4 week 0x81-0x82：Last1-2 week

byte flag_week; //Week flag: flag_week 0-6:(Binary 0000000 means：Sat Fri Thu Wed Tue Mon Sun)

byte hour;

byte minute;

byte sec;

GET_DST_PARAM_TIME special_time； //7 byte

### 2.50.3 Response

【Return value】1：The command was executed successfully.；Minus：Execute command failure

【Actual Data】CChex_Update, Type return CCHEX_RET_SET_DST_PARAM_TYPE

Data Part：

uint MachineId; // Device ID

int Result; // 0:OK, -1:error

### 2.50.4 Sample

int ret = CChex_SetDSTParam(CchexHandle,DevIdx,ref Param);

### 2.50.5 Notice

- Make sure that CChex_Start has been started successfully before run.；

## 2.51 CChex_GetDevExtInfo

### 2.51.1 Description function

【Function】Gets the device extension information code

Read the factory name/Tax number/factory Address

### 2.51.2 Request

【Mode】int CChex_GetDevExtInfo(IntPtr CchexHandle,int DevIdx);

【Parameter】CchexHandle：,successfully create the handle，Input\[Parameter\]；

DevIdx: Search the device，Input\[Parameter\]

### 2.51.3 Response

【Return value】1：The command was executed successfully.；Minus：Execute command failure

【Actual Data】CChex_Update, Type return CCHEX_RET_GET_DEV_EXT_INFO_TYPE

Data Part：

uint MachineId; // Device ID

int Result; // 0:OK, -1:error

CCHEX_SET_DEV_EXT_INFO_STRU param; //320byte

Byte\[50\] manufacturer_name; //Factory name

Byte\[100\] manufacturer_addr; //Address

Byte\[15\] duty_paragraph; //Tax number

Byte\[155\] reserved; //Reserved

### 2.51.4 Sample

int ret = CChex_GetDevExtInfo(CchexHandle,DevIdx);

### 2.51.5 Notice

- Make sure that CChex_Start has been started successfully before run.；

## 2.52 CChex_SetDevExtInfo

### 2.52.1 Description function

【Function】Modify the device extension information code

Modify the factory name/Tax number/factory Address

### 2.52.2 Request

【Mode】int CChex_SetDevExtInfo(IntPtr CchexHandle, int DevIdx,ref CCHEX_SET_DEV_EXT_INFO_STRU Param);

【Parameter】CchexHandle：,successfully create the handle，Input\[Parameter\]；

DevIdx: Search the device，Input\[Parameter\]

Param：320byte，Input\[Parameter\]；

Byte\[50\] manufacturer_name; //factory name

Byte\[100\] manufacturer_addr; //factory address

Byte\[15\] duty_paragraph; //Tax number

Byte\[155\] reserved; //Reserved

### 2.52.3 Response

【Return value】1：The command was executed successfully.；Minus：Execute command failure

【Actual Data】CChex_Update, Type return CCHEX_RET_SET_DEV_EXT_INFO_TYPE

Data Part：

uint MachineId; // Device ID

int Result; // 0:OK, -1:error

### 2.52.4 Sample

int ret = CChex_SetDevExtInfo(CchexHandle,DevIdx,ref Param);

### 2.52.5 Notice

- Make sure that CChex_Start has been started successfully before run.；

## 2.53 CChex_GetBasicConfigInfo3

### 2.53.1Description function

【Function】Get the device configuration information3

Read Wiegand mode。

### 2.53.2 Request

【Mode】int CChex_GetBasicConfigInfo3(IntPtr CchexHandle,int DevIdx);

【Parameter】CchexHandle：,successfully create the handle，Input\[Parameter\]；

DevIdx: Search the device，Input\[Parameter\]

### 2.53.3 Response

【Return value】1：The command was executed successfully.；Minus：Execute command failure

【Actual Data】CChex_Update, Type return CCHEX_RET_GET_BASIC_CFG3_TYPE

Data Part：

uint MachineId; // Device ID

int Result; // 0:OK, -1:error

CCHEX_SET_BASIC_CFG_INFO3_STRU param; //15 byte

Byte wiegand_type; //wiegand mode

Byte online_mode; //online mode

Byte collect_level; //Acquisition threshold

Byte pwd_status; //communication password status =0 ,connect don't need the communication password by TCP/IP; communication password status =1, send 0x04 command, verify the communication password by TCP/IP

Byte sensor_status; //=0 don't report the door sensor status; =1 activate report the door sensor status（the device will activate send the response of 0x2F command )

Byte\[8\] reserved; //Reserved

Byte independent_time; //User independent time limit

Byte m5_t5_status; //= 0 disable; = 1 enable; Out =2, disable In =4 disable，out =5 disable，In

Byte padding; //alignment invalid data

### 2.53.4 Sample

int ret = CChex_GetBasicConfigInfo3(CchexHandle,DevIdx);

### 2.53.5 Notice

- Make sure that CChex_Start has been started successfully before run.；

## 2.54 CChex_SetBasicConfigInfo3

### 2.54.1 Description function

【Function】Setup the device configuration information3

Read Wiegand mode

### 2.54.2 Request

【Mode】int CChex_SetBasicConfigInfo3(IntPtr CchexHandle, int DevIdx,ref CCHEX_SET_BASIC_CFG_INFO3_STRU Config);

【Parameter】CchexHandle：,successfully create the handle，Input\[Parameter\]；

DevIdx: Search the device，Input\[Parameter\]

Config：15byte，Input\[Parameter\]；

Byte wiegand_type; //wiegand mode

Byte online_mode; //Online mode

Byte collect_level; //Acquisition threshold

Byte pwd_status; //communication password status =0 ,connect don't need the communication password by TCP/IP; communication password status =1, send 0x04 command, verify the communication password by TCP/IP

Byte sensor_status; //=0 don't report the door sensor status; =1 activate report the door sensor status（the device will activate send the response of 0x2F command )

Byte\[8\] reserved; //Reserved

Byte independent_time; //User independent time limit

Byte m5_t5_status; //= 0 disable; = 1 enable; Out =2, disable In =4 disable，out =5 disable，In

### 2.54.3 Response

【Return value】1：The command was executed successfully.；Minus：Execute command failure

【Actual Data】CChex_Update, Type return CCHEX_RET_SET_BASIC_CFG3_TYPE

Data Part：

uint MachineId; // Device ID

int Result; // 0:OK, -1:error

### 2.54.4 Sample

int ret = CChex_SetBasicConfigInfo3(CchexHandle,DevIdx,ref Config);

### 2.54.5 Notice

- Make sure that CChex_Start has been started successfully before run.；

## 2.55 CChex_ConnectionAuthentication

### 2.55.1 Description function

【Function】connection identification

connection identification, identification successful, then response the other command; if the identification successful and no data transfer in 5 mins , recover to dis-identification mode

### 2.55.2 Request

【Mode】int CChex_ConnectionAuthentication(IntPtr CchexHandle, int DevIdx,ref CCHEX_CONNECTION_AUTHENTICATION_STRU Param);

【Parameter】CchexHandle：,successfully create the handle，Input\[Parameter\]；

DevIdx: Search the device，Input\[Parameter\]

Param：24byte，Input\[Parameter\]；

Byte\[12\] username;

Byte\[12\] password;

//Iris user name "admin"，password "admin"，

//Other devices, don't identify user name, only identify communication password.

### 2.55.3 Response

【Return value】1：The command was executed successfully.；Minus：Execute command failure

【Actual Data】CChex_Update, Type return CCHEX_RET_CONNECTION_AUTHENTICATION_TYPE

Data Part：

uint MachineId; // Device ID

int Result; // 0:OK, -1:error

### 2.55.4 Sample

int ret = CChex_ConnectionAuthentication(CchexHandle,DevIdx,ref Param);

### 2.55.5 Notice

- Make sure that CChex_Start has been started successfully before run.；

## 2.56 CChex_GetRecordNumByEmployeeIdAndTime

### 2.56.1 Description function

【Function】Get the record quantity by user ID and time. Only support A20/972 hardware platform devices.

Get the record quantity by user ID and time.

### 2.56.2 Request

【Mode】int CChex_GetRecordNumByEmployeeIdAndTime(IntPtr CchexHandle, int DevIdx,ref CCHEX_GET_RECORD_INFO_BY_TIME Param);

【Parameter】CchexHandle：,successfully create the handle，Input\[Parameter\]；

DevIdx: Search the device，Input\[Parameter\]

Param：13byte，Input\[Parameter\]；

Byte\[5\] EmployeeId; //User ID

Byte\[4\] start_date; //The number of seconds after 2000.1.2 Byte\[4\] end_date; //The number of seconds after 2000.1.2

User ID = 0xFF means all users

Start date = 0xFF, means unlimit the start date

End date = 0xFF, means unlimit the end date

### 2.56.3 Response

【Return value】1：The command was executed successfully.；Minus：Execute command failure

【Actual Data】CChex_Update, Type return CCHEX_RET_GET_RECORD_NUMBER_TYPE

Data Part：

uint MachineId; // Device ID

int Result; // 0:OK, -1:error

int record_num; // record quantity

### 2.56.4 Sample

int ret = CChex_GetRecordNumByEmployeeIdAndTime(CchexHandle,DevIdx,ref Param);

### 2.56.5 Notice

- Make sure that CChex_Start has been started successfully before run.；

## 2.57 CChex_DownloadRecordByEmployeeIdAndTime

### 2.57.1 Description function

【Function】Get the record by user ID and time. Only support A20/972 hardware platform devices.

Get the record by user ID and time。

### 2.57.2 Request

【Mode】int CChex_DownloadRecordByEmployeeIdAndTime(IntPtr CchexHandle, int DevIdx,ref CCHEX_GET_RECORD_INFO_BY_TIME Param);

【Parameter】CchexHandle：,successfully create the handle，Input\[Parameter\]；

DevIdx: Search the device，Input\[Parameter\]

Param：13byte，Input\[Parameter\]；

Byte\[12\] EmployeeId; //user ID

Byte\[4\] start_date; //The number of seconds after 2000.1.2

Byte\[4\] end_date; //The number of seconds after 2000.1.2

User ID = 0xFF means all users

Start date = 0xFF, means unlimit the start date

End date = 0xFF, means unlimit the end date

### 2.57.3Response

【Return value】1：The command was executed successfully.；Minus：Execute command failure

【Actual Data】CChex_Update, Type return CCHEX_RET_GET_RECORD_BY_EMPLOYEE_TIME_TYPE

Data Part：

uint MachineId; // Device ID

int Result; // 0:OK, -1:error

Byte\[5\] EmployeeId; //User ID

Byte\[4\] char date; //date time

Byte char back_id; //back ID

Byte ecord_type; //record type

Byte\[3\] work_type; //work code

Byte\[2\] padding; //alignment invalid data

Note: The number of times this type of data is returned based on the number of records recorded during the output download time

### 2.57.4 Sample

int ret = CChex_DownloadRecordByEmployeeIdAndTime(CchexHandle,DevIdx,ref Param);

### 2.57.5 Notice

- Make sure that CChex_Start has been started successfully before run.；

## 2.58 Send attendance records in real time

### 2.58.1 Description function

【Function】Send the records in real time

After verification, the attendance information will be output automatically, and only the response information will be available.

### 2.58.2 Request

【Mode】

【Parameter】

### 2.58.3 Response

【Return value】1：The command was executed successfully.；Minus：Execute command failure

【Actual Data】CChex_Update, Type return CCHEX_RET_LIVE_SEND_ATTENDANCE_TYPE

Data Part：

uint MachineId; // Device ID

int Result; // 0:OK, -1:error

byte\[5\] EmployeeId; //user ID

byte\[4\] timestamp; //The number of seconds after 2000.1.2

Byte backup; //backup ID

byte record_type; //record type

byte\[3\] work_type\[3\]; //workcode

byte\[2\] padding\[2\];

### 2.58.4Sample

### 2.58.5 Notice

- Make sure that CChex_Start has been started successfully before run.；

2\. Set whether the device configuration information 2 is sent in real time

## 2.59 CChex_GetRecordInfo

### 2.59.1 Description function

【Function】Get the record information

Get the record information, contains already register user quantity, already register FP quantity, already register password quantity, all record quantity and new record quantity

### 2.59.2 Request

【Mode】int CChex_GetRecordInfoStatus(IntPtr CchexHandle, int DevIdx);

【Parameter】CchexHandle：,successfully create the handle，Input\[Parameter\]；

DevIdx: Search the device，Input\[Parameter\]

### 2.59.3 Response

【Return value】1：The command was executed successfully.；Minus：Execute command failure

【Actual Data】CChex_Update, Type return CCHEX_RET_GET_RECORD_INFO_STATUS_TYPE

Data Part：

uint MachineId; // Device ID

uint EmployeeNum; //User quantity

uint FingerPrtNum; //FP quantity

uint PasswdNum;

uint CardNum;

uint TotalRecNum;

uint NewRecNum;

### 2.59.4 Sample

int ret = CChex_GetRecordInfoStatus(CchexHandle,DevIdx);

### 2.59.5 Notice

- Make sure that CChex_Start has been started successfully before run.；

## 2.60 CChex_DownloadAllNewRecords

### 2.60.1 Description function

【Function】Download all record

### 2.60.2Request

【Mode】int CChex_DownloadAllNewRecords(IntPtr CchexHandle, int DevIdx);

【Parameter】CchexHandle：,successfully create the handle，Input\[Parameter\]；

DevIdx: Search the device，Input\[Parameter\]

### 2.60.3 Response

【Return value】1：The command was executed successfully.；Minus：Execute command failure

【Actual Data】CChex_Update, Type return CCHEX_RET_GET_NEW_RECORD_INFO_TYPE

Data Part：

uint MachineId; // Device ID

byte NewRecordFlag; // new record or no

byte\[5\] EmployeeId; // User ID，

byte\[4\] Date; // Time，The number of seconds after 2000.1.2

byte BackId; // Backup ID，3 digits：Card，2 digits：，1~0：11，digits 7~4：10 FP templates（1~10）

byte RecordType // Record type，7 digits：1：open the door，0：can't open the door；3~0 digits attendance status

byte\[3\] WorkType; // workcode

byte Rsv // reserved

### 2.60.4 Sample

int ret = CChex_DownloadAllNewRecords(anviz_handle, dev_idx);

### 2.60.5 Notice

- Make sure that CChex_Start has been started successfully before run.；

## 2.61 Upload user information

【Function】upload user information

### 2.61.2 Request

【Mode】call the universal interface **2.20** CChex_ModifyPersonInfo(IntPtr CchexHandle, int DevIdx, ref CCHEX_RET_PERSON_INFO_STRU personlist, byte person_num);

**Call by the device type：dev->DevTypeFlag & 0xff**

Universal 1：dev->DevTypeFlag & 0xff == 0x01

CChex_UploadEmployeeInfo(IntPtr \*CchexHandle, int DevIdx, ref CCHEX_EMPLOYEE_INFO_STRU EmployeeList, byte EmployeeNum);

ASCII universal：dev->DevTypeFlag & 0xff == 0x02

CChex_UploadEmployee2Info(IntPtr \*CchexHandle, int DevIdx, ref CCHEX_EMPLOYEE2_INFO_STRU

EmployeeList, byte EmployeeNum);

UNICODE universal：dev->DevTypeFlag & 0xff == 0x04

CChex_UploadEmployeeInfo(IntPtr \*CchexHandle, int DevIdx, ref CCHEX_EMPLOYEE2UNICODE_INFO_STRU

EmployeeList, byte EmployeeNum);

UNICODE_W2 universal：dev->DevTypeFlag & 0xff == 0x20

CChex_UploadEmployeeInfo(IntPtr \*CchexHandle, int DevIdx, ref CCHEX_EMPLOYEE2W2_INFO_STRU

EmployeeList, byte EmployeeNum);

【Parameter】CchexHandle：,successfully create the handle，Input\[Parameter\]；

DevIdx: Search the device，Input\[Parameter\]

EmployeeNum：Increase the number of employees (8 or 12 per upload per device type and customization)

Universal 1：dev->DevTypeFlag & 0xff == 0x01

CCHEX_EMPLOYEE_INFO_STRU //28byte

byte\[5\] EmployeeId; //user id only one mark 唯一标识

byte\[3\] Passwd; //password

byte\[3\] CardId; //card ID

byte\[10\] EmployeeName; //name

byte DepartmentId; //department ID

byte GroupId; //Group ID

byte Mode; //Attendance mode

byte\[2\] FpStatus; //FP registration status指纹登记情况

byte Special; //条数信息

byte Padding; //无效 结构体对齐

ASCII Universal：dev->DevTypeFlag & 0xff == 0x02

CCHEX_EMPLOYEE2_INFO_STRU //32byte

byte\[5\] EmployeeId; //user id only one mark 唯一标识

byte\[3\] Passwd; //密码

byte\[4\] CardId; //卡号

byte\[10\] EmployeeName; //人名

byte DepartmentId; //部分号

byte GroupId; //组号

byte Mode; //考勤模式

byte\[2\] FpStatus; //指纹登记情况

byte PwdH8bit; //password high 8 bits密码高8位

byte Rserved; //保留

byte Special; //条数信息

Byte\[2\] Padding; //无效 结构体对齐

UNICODE Universal：dev->DevTypeFlag & 0xff == 0x04

CCHEX_EMPLOYEE2UNICODE_INFO_STRU //40byte

byte\[5\] EmployeeId; //员工号 唯一标识

byte\[3\] Passwd; //密码

byte\[4\] CardId; //卡号

byte\[20\] EmployeeName; //人名

byte DepartmentId; //部分号

byte GroupId; //组号

byte Mode; //考勤模式

byte\[2\] FpStatus; //指纹登记情况

byte PwdH8bit; //密码高8位

byte Rserved; //保留

byte Special; //条数信息

UNICODE_W2 Universal：dev->DevTypeFlag & 0xff == 0x20

CCHEX_EMPLOYEE2W2_INFO_STRU

CCHEX_EMPLOYEE2UNICODE_INFO_STRU //40byte

byte\[5\] EmployeeId; //员工号 唯一标识

byte\[3\] Passwd; //密码

byte\[4\] CardId; //卡号

byte\[20\] EmployeeName; //人名

byte DepartmentId; //部分号

byte GroupId; //组号

byte Mode; //考勤模式

byte\[2\] FpStatus; //指纹登记情况

byte PwdH8bit; //密码高8位

byte Rserved; //保留

byte Special; //条数信息

byte\[4\] start_date //员工有效开始时间 2000年1月2日开始的秒数

byte\[4\] end_date //员工有效结束时间 2000年1月2日开始的秒数

### 2.61.3 Response

【Return value】1：The command was executed successfully.；Minus：Execute command failure

【Actual Data】CChex_Update, Type return

Universal 1：dev->DevTypeFlag & 0xff == 0x01

CCHEX_RET_ULEMPLOYEE_INFO_TYPE

ASCII Universal ：dev->DevTypeFlag & 0xff == 0x02

CCHEX_RET_ULEMPLOYEE2_INFO_TYPE

UNICODE Universal：dev->DevTypeFlag & 0xff == 0x04

CCHEX_RET_ULEMPLOYEE2UNICODE_INFO_TYPE

UNICODE_W2 Universal：dev->DevTypeFlag & 0xff == 0x20

CCHEX_RET_ULEMPLOYEE2W2_INFO_TYPE

Data Part：

uint MachineId; // Device ID

int Result; // 0:OK, -1:error

### 2.61.4 Sample

int ret = CChex_UploadEmployeeInfo(anviz_handle, dev_idx，EmployeeList，1);

### 2.61.5 Notice

- Make sure that CChex_Start has been started successfully before run.；