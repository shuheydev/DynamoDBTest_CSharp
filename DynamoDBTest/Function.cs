using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Alexa.NET.Request;
using Alexa.NET.Response;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;


// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace DynamoDBTest
{
    public class Function
    {
        /// <summary>
        /// A simple function that takes a string and does a ToUpper
        /// </summary>
        /// <param name="input"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public SkillResponse FunctionHandler(SkillRequest input, ILambdaContext context)
        {
            //ここにDynamoDBにアクセスするコードを追加していく
            string tableName = "TestTable";//テーブル名
            AmazonDynamoDBClient client = new AmazonDynamoDBClient();//主役。これを介してDynamoDBにアクセスする

            //テーブルを作成
            CreateTable(client,tableName);

            return new SkillResponse
            {
                Version = "1.0",
                Response = new ResponseBody()
            };
        }


        /// <summary>
        /// DynamoDBにテーブルを作成する
        /// </summary>
        /// <param name="client"></param>
        /// <param name="tableName"></param>
        private void CreateTable(IAmazonDynamoDB client,string tableName)
        {

            //リクエストを構築
            var request = new CreateTableRequest
            {
                //テーブルの列情報を設定
                //「ThisIsId」と「ThisIsSomthing」という2つの列を持つテーブルを作る
                AttributeDefinitions = new List<AttributeDefinition>()
                {
                    new AttributeDefinition
                    {
                        AttributeName = "ThisIsId",//カラム名
                        AttributeType = "N"
                    },
                    new AttributeDefinition
                    {
                        AttributeName = "ThisIsSomething",//カラム名
                        AttributeType = "N"
                    }
                },
                //勉強中
                KeySchema = new List<KeySchemaElement>
                {
                    new KeySchemaElement
                    {
                        AttributeName = "ThisIsId",
                        KeyType = "HASH" //Partition key
                    },
                    new KeySchemaElement
                    {
                        AttributeName = "ThisIsSomething",
                        KeyType = "RANGE" //Sort key
                    }
                },
                //勉強中
                ProvisionedThroughput = new ProvisionedThroughput
                {
                    ReadCapacityUnits = 5,
                    WriteCapacityUnits = 5
                },
                //テーブル名
                TableName = tableName
            };

            //テーブル作成リクエストを投げる！
            //ただし、非同期メソッドの返りを待たねければならない。
            //待たないと先にこのLambdaが終了して、DynamoDBのテーブル作成処理を完了せずに終わる。
            //でもメソッドにasyncつけたら、最終的にスキルのレスポンスの型もTask<SkillResponse>にしなきゃいけなくなってだめ。
            //.Wait()メソッドで非同期メソッドを同期メソッドにしちゃえば、返り値も変えなくていいし、テーブル作成完了まで待つことができる。
            //一回完結のサーバー側の処理で非同期でなきゃいけない理由ないしね。
            client.CreateTableAsync(request).Wait();
        }
    }
}
