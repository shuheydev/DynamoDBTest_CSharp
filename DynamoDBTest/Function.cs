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
            //������DynamoDB�ɃA�N�Z�X����R�[�h��ǉ����Ă���
            string tableName = "TestTable";//�e�[�u����
            AmazonDynamoDBClient client = new AmazonDynamoDBClient();//����B��������DynamoDB�ɃA�N�Z�X����

            //�����e�[�u���̑��݊m�F
            if (!IsTableExist(client, tableName))
            {
                //�e�[�u�����쐬
                CreateTable(client, tableName);
            }

            PutItem(client, tableName);

            return new SkillResponse
            {
                Version = "1.0",
                Response = new ResponseBody()
            };
        }


        /// <summary>
        /// DynamoDB�Ƀe�[�u�����쐬����
        /// </summary>
        /// <param name="client"></param>
        /// <param name="tableName"></param>
        private void CreateTable(IAmazonDynamoDB client, string tableName)
        {
            //���N�G�X�g���\�z
            var request = new CreateTableRequest
            {
                //�e�[�u���̗����ݒ�
                //�uThisIsId�v�ƁuThisIsSomthing�v�Ƃ���2�̗�����e�[�u�������
                AttributeDefinitions = new List<AttributeDefinition>()
                {
                    new AttributeDefinition
                    {
                        AttributeName = "ThisIsId",//�J������
                        AttributeType = "N"//�f�[�^�̃^�C�v�FN�F���l�AS�F������A���ɂ��������B�׋���
                    },
                    new AttributeDefinition
                    {
                        AttributeName = "ThisIsSomething",//�J������
                        AttributeType = "N"//�f�[�^�̃^�C�v�FN�F���l�AS�F������A���ɂ��������B�׋���
                    }
                },
                //�׋���
                KeySchema = new List<KeySchemaElement>
                {
                    new KeySchemaElement
                    {
                        AttributeName = "ThisIsId",
                        KeyType = KeyType.HASH //Partition key
                    },
                    new KeySchemaElement
                    {
                        AttributeName = "ThisIsSomething",
                        KeyType = KeyType.RANGE,//Sort key
                    }
                },
                //�׋���
                ProvisionedThroughput = new ProvisionedThroughput
                {
                    ReadCapacityUnits = 5,
                    WriteCapacityUnits = 5
                },
                //�e�[�u����
                TableName = tableName
            };

            //�e�[�u���쐬���N�G�X�g�𓊂���I
            //�������A�񓯊����\�b�h�̕Ԃ��҂��˂���΂Ȃ�Ȃ��B
            //�҂��Ȃ��Ɛ�ɂ���Lambda���I�����āADynamoDB�̃e�[�u���쐬���������������ɏI���B
            //�ł����\�b�h��async������A�ŏI�I�ɃX�L���̃��X�|���X�̌^��Task<SkillResponse>�ɂ��Ȃ��Ⴂ���Ȃ��Ȃ��Ă��߁B
            //.Wait()���\�b�h�Ŕ񓯊����\�b�h�𓯊����\�b�h�ɂ����Ⴆ�΁A�Ԃ�l���ς��Ȃ��Ă������A�e�[�u���쐬�����܂ő҂��Ƃ��ł���B
            //��񊮌��̃T�[�o�[���̏����Ŕ񓯊��łȂ��Ⴂ���Ȃ����R�Ȃ����ˁB
            //client.CreateTableAsync(request).Wait();

            //Wait���\�b�h�ł͂����񓯊����\�b�h�̊�����҂����ł������A�񓯊����\�b�h�̕Ԃ�l���擾�������ꍇ�́AResult�v���p�e�B���g���܂��傤�B
            //Result�v���p�e�B�ɃA�N�Z�X���邱�ƂŁA�񓯊����\�b�h�̊�����҂�����Ō��ʂ��擾���邱�Ƃ��ł��܂��B
            //���ʂ��g���ĉ����������s�������ꍇ�͂����炪�ǂ��̂ł͂Ȃ��ł��傤���B
            var result = client.CreateTableAsync(request).Result;
        }


        /// <summary>
        /// �����̃e�[�u�������݂��邩���`�F�b�N���܂��B
        /// </summary>
        /// <param name="client"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        private bool IsTableExist(IAmazonDynamoDB client, string tableName)
        {
            //�e�[�u���ꗗ���擾
            var tableList = client.ListTablesAsync().Result;
            //TableNames�v���p�e�B���`�F�b�N
            return tableList.TableNames.Exists(s => s.Equals(tableName));
        }


        /// <summary>
        /// �f�[�^���e�[�u���ɒǉ����܂��B
        /// </summary>
        /// <param name="client"></param>
        /// <param name="tableName"></param>
        private void PutItem(IAmazonDynamoDB client, string tableName)
        {
            //���N�G�X�g�̍\�z
            var request = new PutItemRequest
            {
                TableName = tableName,//�ǉ���̃e�[�u����
                //�e�J�����̒l���w��
                Item = new Dictionary<string, AttributeValue>
                {
                    {"ThisIsId",new AttributeValue{N= "2"} },
                    {"ThisIsSomething",new AttributeValue{N="5"} }
                }
            };

            //�e�[�u���ɒǉ�
            var result = client.PutItemAsync(request).Result;
        }
    }
}
