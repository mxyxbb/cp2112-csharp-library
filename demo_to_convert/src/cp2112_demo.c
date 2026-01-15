#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <windows.h>
#include "smbus.h"

#define BITRATE_HZ                  100000
#define ACK_ADDRESS                 0x02
#define AUTO_RESPOND                FALSE
#define WRITE_TIMEOUT_MS            10
#define READ_TIMEOUT_MS             10
#define TRANSFER_RETRIES            0
#define SCL_LOW_TIMEOUT             TRUE
#define RESPONSE_TIMEOUT_MS         100

#define CHARGER_SLAVE_ADDRESS_W     0x12
#define BATTERY_SLAVE_ADDRESS_W     0x16
#define LVDC4816_SLAVE_ADDRESS_W    0xC0
#define LVDC4816_SLAVE_ADDRESS0x60_W    0xC8
#define LVDC4816_SLAVE_ADDRESS0x64_W    0xC8

INT16 MFRversion_raw;
INT16 HWOCP_raw;
float HWOCP_A;
INT16 temperature1_raw;
float temperature1_C;
INT16 temperature2_raw;
float temperature2_C;
INT16 HVvoltage_raw;
float HVvoltage_V;
INT16 LVvoltage_raw;
float LVvoltage_V;
INT16 I2_current_raw;
float I2_current_A;
INT16 I1_current_raw;
float I1_current_A;
INT16 I1_CNT;
INT16 DUT_Status;
int first_timeA = 0;
int first_timeB = 0;

int main(int argc, char* argv[])
{
    HID_SMBUS_DEVICE    m_hidSmbus;
    BYTE                buffer[HID_SMBUS_MAX_READ_RESPONSE_SIZE];
    BYTE                targetAddress[16];
    WORD                regLength;
    // Open device
    if(SMBus_Open(&m_hidSmbus) != 0)
    {
        fprintf(stderr,"\r\nERROR: Could not open device.\r\n");
        SMBus_Close(m_hidSmbus);
        fprintf(stderr,"Enter to exit...");
        getchar();
        return -1;
    }
    fprintf(stderr,"\r\nDevice successfully opened.\r\n");

    // Configure device
    if(SMBus_Configure(m_hidSmbus, BITRATE_HZ, ACK_ADDRESS, AUTO_RESPOND, WRITE_TIMEOUT_MS, READ_TIMEOUT_MS, SCL_LOW_TIMEOUT, TRANSFER_RETRIES, RESPONSE_TIMEOUT_MS) != 0)
    {
        fprintf(stderr,"ERROR: Could not configure device.\r\n");
        SMBus_Close(m_hidSmbus);
        fprintf(stderr,"Enter to exit...");
        getchar();
        return -1;
    }
    fprintf(stderr,"Device successfully configured.\r\n");

    // MFRversion [0x9B]
    targetAddress[0] = 0x9B;
    regLength = 2;
    if (SMBus_Read(m_hidSmbus, buffer, LVDC4816_SLAVE_ADDRESS0x60_W, regLength, 1, targetAddress) != regLength)
    {
        fprintf(stderr,"ERROR: Could not perform SMBus read 'MFRversion' Reg  %02X\r\n", targetAddress[0]);
        // SMBus_Close(m_hidSmbus);
        // return -1;
    }
    MFRversion_raw = (buffer[1] << 8) | buffer[0];
    MFRversion_raw = MFRversion_raw & 0xFFFF; // 16-bit 2's complement
    fprintf(stderr, "MFRversion=0x%x\r\n", MFRversion_raw);

    // HW OCP [0xEA]
    targetAddress[0] = 0xEA;
    regLength = 2;
    if (SMBus_Read(m_hidSmbus, buffer, LVDC4816_SLAVE_ADDRESS0x60_W, regLength, 1, targetAddress) != regLength)
    {
        fprintf(stderr,"ERROR: Could not perform SMBus read 'HW_OCP' Reg %02X\r\n", targetAddress[0]);
        // SMBus_Close(m_hidSmbus);
        // return -1;
    }
    HWOCP_raw = (buffer[1] << 8) | buffer[0];
    HWOCP_raw = HWOCP_raw & 0xFFFF; // 16-bit 2's complement
    HWOCP_A = HWOCP_raw / 32.0f;
    fprintf(stderr, "HWOCP=%2.2f\r\n", HWOCP_A);

    // Write protect [0x10]
    targetAddress[0] = 0x10;
    buffer[0] = 0x10;
    buffer[1] = 0;
    regLength = 2;
    if (SMBus_Write(m_hidSmbus, buffer, LVDC4816_SLAVE_ADDRESS0x60_W, regLength))
    {
        fprintf(stderr,"ERROR: Could not perform SMBus write 'Write protect' Reg = %02X\r\n", targetAddress[0]);
        // SMBus_Close(m_hidSmbus);
        // return -1;
    }

    // HW OCP [0xEA]
    targetAddress[0] = 0xEA;
    HWOCP_A = 600;
    buffer[0] = 0xEA;
    buffer[1] = ((UINT8)(HWOCP_A*32)&0xff);
    buffer[2] = (UINT8)(((UINT16)(HWOCP_A*32)>>8)&0xff); // Set HW OCP to 600A
    fprintf(stderr, "Setting HWOCP to %d \r\n", buffer[1]);
    fprintf(stderr, "Setting HWOCP to %d \r\n", buffer[2]);
        
    regLength = 3;
    if (SMBus_Write(m_hidSmbus, buffer, LVDC4816_SLAVE_ADDRESS0x60_W, regLength))
    {
        fprintf(stderr,"ERROR: Could not perform SMBus write. Reg = %02X\r\n", targetAddress[0]);
        // SMBus_Close(m_hidSmbus);
        // return -1;
    }

    // HW OCP [0xEA]
    targetAddress[0] = 0xEA;
    regLength = 2;
    if (SMBus_Read(m_hidSmbus, buffer, LVDC4816_SLAVE_ADDRESS0x60_W, regLength, 1, targetAddress) != regLength)
    {
        fprintf(stderr,"ERROR: Could not perform SMBus read. Reg = %02X\r\n", targetAddress[0]);
        // SMBus_Close(m_hidSmbus);
        // return -1;
    }
    HWOCP_raw = (buffer[1] << 8) | buffer[0];
    HWOCP_raw = HWOCP_raw & 0xFFFF; // 16-bit 2's complement
    HWOCP_A = HWOCP_raw / 32.0f;
    fprintf(stderr, "HWOCP=%2.2f\r\n", HWOCP_A);

    while(1)
    {
        buffer[0] = 0;
        buffer[1] = 0;
        // Temperature1 [0x8D]
        targetAddress[0] = 0x8D;
        regLength = 2;
        if (SMBus_Read(m_hidSmbus, buffer, LVDC4816_SLAVE_ADDRESS0x60_W, regLength, 1, targetAddress) != regLength)
        {
            fprintf(stderr,"ERROR: Could not perform SMBus read. Reg = %02X\r\n", targetAddress[0]);
            // SMBus_Close(m_hidSmbus);
            // return -1;
        }
        temperature1_raw = (buffer[1] << 8) | buffer[0];
        temperature1_raw = temperature1_raw & 0xFFFF; // 16-bit 2's complement
        temperature1_C = temperature1_raw / 32.0f - 40.0f;

        // Temperature2 [0x8E]
        targetAddress[0] = 0x8E;
        regLength = 2;
        if (SMBus_Read(m_hidSmbus, buffer, LVDC4816_SLAVE_ADDRESS0x60_W, regLength, 1, targetAddress) != regLength)
        {
            fprintf(stderr,"ERROR: Could not perform SMBus read. Reg = %02X\r\n", targetAddress[0]);
            // SMBus_Close(m_hidSmbus);
            // return -1;
        }
        temperature2_raw = (buffer[1] << 8) | buffer[0];
        temperature2_raw = temperature2_raw & 0xFFFF; // 16-bit 2's complement
        temperature2_C = temperature2_raw / 32.0f - 40.0f;

        // HV [0x88]
        targetAddress[0] = 0x88;
        regLength = 2;
        if (SMBus_Read(m_hidSmbus, buffer, LVDC4816_SLAVE_ADDRESS0x60_W, regLength, 1, targetAddress) != regLength)
        {
            fprintf(stderr,"ERROR: Could not perform SMBus read. Reg = %02X\r\n", targetAddress[0]);
            // SMBus_Close(m_hidSmbus);
            // return -1;
        }
        HVvoltage_raw = (buffer[1] << 8) | buffer[0];
        HVvoltage_raw = HVvoltage_raw & 0xFFFF; // 16-bit 2's complement
        HVvoltage_V = HVvoltage_raw / 32.0f;

        // LV [0x8B]
        targetAddress[0] = 0x8B;
        regLength = 2;
        if (SMBus_Read(m_hidSmbus, buffer, LVDC4816_SLAVE_ADDRESS0x60_W, regLength, 1, targetAddress) != regLength)
        {
            fprintf(stderr,"ERROR: Could not perform SMBus read. Reg = %02X\r\n", targetAddress[0]);
            // SMBus_Close(m_hidSmbus);
            // return -1;
        }
        LVvoltage_raw = (buffer[1] << 8) | buffer[0];
        LVvoltage_raw = LVvoltage_raw & 0xFFFF; // 16-bit 2's complement
        LVvoltage_V = LVvoltage_raw / 32.0f;

        // I2 Current [0x8C]
        targetAddress[0] = 0x8C;
        regLength = 2;
        if (SMBus_Read(m_hidSmbus, buffer, LVDC4816_SLAVE_ADDRESS0x60_W, regLength, 1, targetAddress) != regLength)
        {
            fprintf(stderr,"ERROR: Could not perform SMBus read. Reg = %02X\r\n", targetAddress[0]);
            // SMBus_Close(m_hidSmbus);
            // return -1;
        }
        I2_current_raw = (buffer[1] << 8) | buffer[0];
        I2_current_raw = I2_current_raw & 0xFFFF; // 16-bit 2's complement
        I2_current_A = I2_current_raw / 32.0f;

        // I1 Current [0x90]
        targetAddress[0] = 0x90;
        regLength = 2;
        if (SMBus_Read(m_hidSmbus, buffer, LVDC4816_SLAVE_ADDRESS0x60_W, regLength, 1, targetAddress) != regLength)
        {
            fprintf(stderr,"ERROR: Could not perform SMBus read. Reg = %02X\r\n", targetAddress[0]);
            // SMBus_Close(m_hidSmbus);
            // return -1;
        }
        I1_current_raw = (buffer[1] << 8) | buffer[0];
        I1_current_raw = I1_current_raw & 0xFFFF; // 16-bit 2's complement
        I1_current_A = I1_current_raw / 32.0f;

        // I1 CNT [0xCD]
        targetAddress[0] = 0xCD;
        regLength = 2;
        if (SMBus_Read(m_hidSmbus, buffer, LVDC4816_SLAVE_ADDRESS0x60_W, regLength, 1, targetAddress) != regLength)
        {
            fprintf(stderr,"ERROR: Could not perform SMBus read. Reg = %02X\r\n", targetAddress[0]);
            // SMBus_Close(m_hidSmbus);
            // return -1;
        }
        I1_CNT = (buffer[1] << 8) | buffer[0];
        I1_CNT = I1_CNT & 0xFFFF; // 16-bit 2's complement
        
        // Status [0x79]
        targetAddress[0] = 0x79;
        regLength = 2;
        if (SMBus_Read(m_hidSmbus, buffer, LVDC4816_SLAVE_ADDRESS0x60_W, regLength, 1, targetAddress) != regLength)
        {
            fprintf(stderr,"ERROR: Could not perform SMBus read. Reg = %02X\r\n", targetAddress[0]);
            // SMBus_Close(m_hidSmbus);
            // return -1;
        }
        DUT_Status = (buffer[1] << 8) | buffer[0];
        DUT_Status = DUT_Status & 0xFFFF; // 16-bit 2's complement
        
        fprintf(stderr, "HV_V=%2.2f, LV_V=%2.2f, I1_A=%2.2f, I2_A=%2.2f, Temp1_C=%2.2f, Temp2_C=%2.2f, I1_CNT=%d, DUT_Status=0x%x\r\n", HVvoltage_V, LVvoltage_V, I1_current_A, I2_current_A, temperature1_C, temperature2_C, I1_CNT, DUT_Status);
        //openfile("output.csv","a") and then write data
        first_timeA ++;
        FILE *fp;
        fp = fopen("outputA.csv", "a");
        if(first_timeA == 1)
            fprintf(fp, "HV_V,LV_V,I1_A,I2_A,Temp1_C,Temp2_C,I1_CNT,DUT_Status\n");
        fprintf(fp, "%2.2f,%2.2f,%2.2f,%2.2f,%2.2f,%2.2f,%d,0x%x\n", HVvoltage_V, LVvoltage_V, I1_current_A, I2_current_A, temperature1_C, temperature2_C, I1_CNT, DUT_Status);
        fclose(fp);
        // Sleep for 0.5 second
        Sleep(500);
    }

    // Success
    fprintf(stderr, "Done! Exiting...\r\n");
    SMBus_Close(m_hidSmbus);
    return 0;
}
