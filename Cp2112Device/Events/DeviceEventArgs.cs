using System;
using Cp2112Sdk.Models;

namespace Cp2112Sdk.Events
{
    /// <summary>
    /// Event arguments for device state changes
    /// </summary>
    public class DeviceStateEventArgs : EventArgs
    {
        public DeviceState PreviousState { get; }
        public DeviceState CurrentState { get; }
        public string Message { get; }

        public DeviceStateEventArgs(DeviceState previousState, DeviceState currentState, string message = null)
        {
            PreviousState = previousState;
            CurrentState = currentState;
            Message = message ?? $"State changed from {previousState} to {currentState}";
        }

        public override string ToString()
        {
            return $"DeviceState: {PreviousState} -> {CurrentState}{(Message != null ? $" ({Message})" : "")}";
        }
    }

    /// <summary>
    /// Event arguments for data received events
    /// </summary>
    public class DataReceivedEventArgs : EventArgs
    {
        public byte SlaveAddress { get; }
        public byte[] Data { get; }
        public int BytesRead { get; }

        public DataReceivedEventArgs(byte slaveAddress, byte[] data, int bytesRead)
        {
            SlaveAddress = slaveAddress;
            Data = data;
            BytesRead = bytesRead;
        }

        public override string ToString()
        {
            return $"DataReceived from 0x{SlaveAddress:X2}: {BytesRead} bytes";
        }
    }

    /// <summary>
    /// Event arguments for data sent events
    /// </summary>
    public class DataSentEventArgs : EventArgs
    {
        public byte SlaveAddress { get; }
        public byte[] Data { get; }
        public int BytesSent { get; }

        public DataSentEventArgs(byte slaveAddress, byte[] data, int bytesSent)
        {
            SlaveAddress = slaveAddress;
            Data = data;
            BytesSent = bytesSent;
        }

        public override string ToString()
        {
            return $"DataSent to 0x{SlaveAddress:X2}: {BytesSent} bytes";
        }
    }

    /// <summary>
    /// Event arguments for error events
    /// </summary>
    public class ErrorEventArgs : EventArgs
    {
        public string ErrorMessage { get; }
        public int ErrorCode { get; }
        public Exception Exception { get; }
        public bool IsFatal { get; }

        public ErrorEventArgs(string errorMessage, int errorCode = 0, Exception exception = null, bool isFatal = false)
        {
            ErrorMessage = errorMessage;
            ErrorCode = errorCode;
            Exception = exception;
            IsFatal = isFatal;
        }

        public override string ToString()
        {
            return $"Error{(IsFatal ? " (Fatal)" : "")}: {ErrorMessage} (Code: 0x{ErrorCode:X2})";
        }
    }

    /// <summary>
    /// Event arguments for transfer status changes
    /// </summary>
    public class TransferStatusEventArgs : EventArgs
    {
        public TransferStatus Status { get; }

        public TransferStatusEventArgs(TransferStatus status)
        {
            Status = status;
        }

        public override string ToString()
        {
            return $"Transfer Status: S0={Status.Status0:X2}, S1={Status.Status1:X2}, Retries={Status.NumRetries}, Bytes={Status.BytesRead}";
        }
    }
}
