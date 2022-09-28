using System;
using System.Text;
using Chaos.NaCl;
using Org.BouncyCastle.Security;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Wallet.Utilities;
using UnityEngine;
using UnityEngine.Networking;

// ReSharper disable once CheckNamespace

namespace Solana.Unity.SDK
{
    public static class Utils
    {
        /// <summary>
        /// Create random byte of the specified size
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        public static byte[] GenerateRandomBytes(int size)
        {
            var buffer = new byte[size];
            new SecureRandom().NextBytes(buffer);
            return buffer;
        }

        /// <summary>
        /// Create DeepLink URL for logging in to Phantom and redirect to the game
        /// </summary>
        /// <param name="redirectScheme">The schema registered in Unity for DeepLink</param>
        /// <param name="metadataUrl">MetadataUrl, will be used by phantom to render game information</param>
        /// <param name="apiVersion">Phantom API version</param>
        /// <param name="connectionPublicKey">PublicKey used for establish connection with Phantom</param>
        /// <param name="cluster">The solana cluster</param>
        /// <returns></returns>
        public static string CreateLoginDeepLink(
            string redirectScheme, string metadataUrl, string apiVersion,
            string connectionPublicKey, RpcCluster cluster)
        {
            var redirectUri = UnityWebRequest.EscapeURL($"{redirectScheme}://onPhantomConnected");
            return $"https://phantom.app/ul/{apiVersion}/connect?app_url=" +
                   $"{metadataUrl}&dapp_encryption_public_key=" +
                   $"{connectionPublicKey}" +
                   $"&redirect_link={redirectUri}&cluster={GetClusterString(cluster)}";
        }

        
        /// <summary>
        /// Create DeepLink URL for signing a transaction with Phantom and redirect to the game
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="phantomEncryptionPubKey"></param>
        /// <param name="phantomConnectionAccountPrivateKey"></param>
        /// <param name="sessionId"></param>
        /// <param name="redirectScheme"></param>
        /// <param name="apiVersion"></param>
        /// <param name="connectionPublicKey"></param>
        /// <param name="cluster"></param>
        /// <returns></returns>
        public static string CreateSignTransactionDeepLink(
            Transaction transaction, 
            byte[] phantomEncryptionPubKey, byte[] phantomConnectionAccountPrivateKey, 
            string sessionId, string redirectScheme, string apiVersion,
            string connectionPublicKey, RpcCluster cluster)
        {
            
            var redirectUri = $"{redirectScheme}://transactionSigned";
            var base58Transaction = Encoders.Base58.EncodeData(transaction.Serialize());
            var transactionPayload = new PhantomTransactionPayload(base58Transaction, sessionId);
            var transactionPayloadJson = JsonUtility.ToJson(transactionPayload);
            var bytesJson = Encoding.UTF8.GetBytes(transactionPayloadJson);
            var randomNonce = Utils.GenerateRandomBytes(24);
            var k = MontgomeryCurve25519.KeyExchange(phantomEncryptionPubKey, phantomConnectionAccountPrivateKey);
            var encryptedMessage = XSalsa20Poly1305.Encrypt(bytesJson, k, randomNonce);
            var base58Payload = Encoders.Base58.EncodeData(encryptedMessage);
            return $"https://phantom.app/ul/{apiVersion}/signTransaction?d" +
                   $"app_encryption_public_key={connectionPublicKey}" +
                   $"&redirect_link={redirectUri}" +
                   $"&nonce={Encoders.Base58.EncodeData(randomNonce)}" +
                   $"&payload={base58Payload}" +
                   $"&cluster={GetClusterString(cluster)}";
        }
        
        public static string GetClusterString(RpcCluster rpcCluster)
        {
            return rpcCluster switch
            {
                RpcCluster.MainNet => "mainnet-beta",
                RpcCluster.DevNet => "devnet",
                RpcCluster.TestNet => "testnet",
                _ => "mainnet-beta"
            };
        }
    }
    
    [Serializable]
    public class PhantomWalletError
    {
        public string errorCode;
        public string errorMessage;
    }

    [Serializable]
    public class PhantomWalletConnectSuccess
    {
        public string public_key;
        public string session;
    }

    [Serializable]
    public class PhantomWalletTransactionSuccessful
    {
        public string signature;
    }
        
    [Serializable]
    public class PhantomWalletTransactionSignedSuccessfully
    {
        public string transaction;
    }
}