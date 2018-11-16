using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Alexa.NET.Request;
using Alexa.NET.Request.Type;
using Alexa.NET.Response;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
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
            string tableName = "TestTable"; //テーブル名
            AmazonDynamoDBClient client = new AmazonDynamoDBClient(); //主役。これを介してDynamoDBにアクセスする

            //同名テーブルの存在確認
            if (!IsTableExist(client, tableName))
            {
                //テーブルを作成
                CreateTable(client, tableName);
            }



            //var intentRequest = input.Request as IntentRequest;
            //var sign = intentRequest.Intent.Slots["StarSign"].Value;

            ////ユーザーIDを`intentRequest`から取得
            //var userId = input.Session.User.UserId;


            #region AmazonDynamoDBClientを使ったデータ追加

            //永続アトリビュートの構築
            var attrs = new AttributeValue();
            //こんな感じで、任意のデータをキーバリューペアの形で追加していく。
            //この「attrs」変数を「attributes」列の値として、あとでリクエストを構築する。
            attrs.M.Add("sign", new AttributeValue { S = "ふたご座" });

            //リクエストの構築
            var request = new PutItemRequest
            {
                TableName = tableName,//追加先のテーブル名
                //各カラムの値を指定
                Item = new Dictionary<string, AttributeValue>
                {
                    {"id",new AttributeValue{S= "testUser"} },
                    {"attributes",attrs}
                }
            };

            ////テーブルに追加
            var result = client.PutItemAsync(request).Result;

            #endregion


            #region Tableを使ったデータの追加

            //目的のテーブルを取得
            var table = Table.LoadTable(client, tableName);

            //データはAttributeValueではなくDocumentを使って構築する。
            //attributes列に入れるデータの構築
            var attr2 = new Document();
            attr2["sign"] = "ふたご座";//文字列でも
            attr2["number"] = 5;//整数でも
                               
            //気にせずに入れられる。

            //試しにList<string>を入れてみる
            var list = new List<string>
            {
                "hello1",
                "hello2",
            };
            attr2["list"] = list;

            //挿入するレコードの構築
            var item = new Document();
            item["id"] = "testuser2";//id列
            item["attributes"] = attr2;//attributes列。上で作成したattr2を入れる。

            var result2 = table.PutItemAsync(item).Result;

            #endregion






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
        private void CreateTable(IAmazonDynamoDB client, string tableName)
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
                        AttributeName = "id",//カラム名
                        AttributeType = "S"//データのタイプ：N：数値、S：文字列、他にもいくつか。
                    },
                    //new AttributeDefinition
                    //{
                    //    AttributeName = "attributes",//カラム名
                    //    AttributeType = "S"//データのタイプ：N：数値、S：文字列、他にもいくつか。
                    //}
                },
                //勉強中
                KeySchema = new List<KeySchemaElement>
                {
                    new KeySchemaElement
                    {
                        AttributeName = "id",
                        KeyType = KeyType.HASH //Partition key
                    },
                    //new KeySchemaElement
                    //{
                    //    AttributeName = "attributes",
                    //    KeyType = KeyType.RANGE,//Sort key
                    //}
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
            //client.CreateTableAsync(request).Wait();

            //Waitメソッドではただ非同期メソッドの完了を待つだけでしたが、非同期メソッドの返り値を取得したい場合は、Resultプロパティを使いましょう。
            //Resultプロパティにアクセスすることで、非同期メソッドの完了を待った上で結果を取得することができます。
            //結果を使って何か処理を行いたい場合はこちらが良いのではないでしょうか。
            var result = client.CreateTableAsync(request).Result;
        }


        /// <summary>
        /// 同名のテーブルが存在するかをチェックします。
        /// </summary>
        /// <param name="client"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        private bool IsTableExist(IAmazonDynamoDB client, string tableName)
        {
            //テーブル一覧を取得
            var tableList = client.ListTablesAsync().Result;
            //TableNamesプロパティをチェック
            return tableList.TableNames.Exists(s => s.Equals(tableName));
        }


        /// <summary>
        /// データをテーブルに追加します。
        /// </summary>
        /// <param name="client"></param>
        /// <param name="tableName"></param>
        private void PutItem(IAmazonDynamoDB client, string tableName, string userId, AttributeValue attrval)
        {
            //リクエストの構築
            var request = new PutItemRequest
            {
                TableName = tableName,//追加先のテーブル名
                //各カラムの値を指定
                Item = new Dictionary<string, AttributeValue>
                {
                    {"id",new AttributeValue{S= userId} },
                    {"attributes",attrval}
                }
            };

            //テーブルに追加
            var result = client.PutItemAsync(request).Result;
        }
    }
}
