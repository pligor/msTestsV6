namespace MyTests.test_graphql;

using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using static MyTests.libs.GenericHelper;
using Microsoft.CSharp.RuntimeBinder;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;


// using System.Linq;

//https://docs.google.com/spreadsheets/d/1HBEOBBRXWVVaOdhFYU8wnWMA8I4NmBM1nTVWqG7_pFg/edit?gid=0#gid=0


//node graphql_ws.js & node mocker_multiple.js &
//dotnet test --filter FullyQualifiedName~TestGraphQL

[TestClass]
public class TestGraphQL {
  private readonly HttpClientHandler handler = new() {
    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
  };

  private GraphQLHttpClient get_client(string path) {
    // works with this server: node mocker_multiple.js

    return new GraphQLHttpClient(new GraphQLHttpClientOptions {
      EndPoint = new Uri($"http://localhost:4000/{path}"),
      HttpMessageHandler = handler
    },
      new NewtonsoftJsonSerializer()
    );
  }

  [TestInitialize]
  public Task InitializeAsync() {
    // throw new NotImplementedException();
    return Task.CompletedTask;
  }

  [TestCleanup]
  public Task DisposeAsync() {
    // throw new NotImplementedException();
    return Task.CompletedTask;
  }

  [TestMethod]
  // [Fact(Timeout = 15000)]
  public async Task test13() {
    // works with this server: node graphql_ws.js
    var graphQLHttpClientOptions = new GraphQLHttpClientOptions {
      EndPoint = new Uri("http://localhost:4001/graphql"),
      UseWebSocketForQueriesAndMutations = true
    };

    var client = new GraphQLHttpClient(
      graphQLHttpClientOptions,
      new NewtonsoftJsonSerializer()
    );

    client.WebSocketReceiveErrors.Subscribe(error => {
      Console.WriteLine($"WebSocket error: {error.Message}");
    });

    var subscriptionRequest = new GraphQLRequest {
      Query = @"
            subscription OnPostCreated {
              postCreated {
                id
                title
                content
              }
            }
            "
    };

    var flag_received_at_least_one_message = false;

    var id_list = new List<int>();

    var subscriptionStream = client.CreateSubscriptionStream<dynamic>(subscriptionRequest);
    subscriptionStream.Subscribe(response => {
      // var post = response.Data.postCreated;
      // var is_valid = is_valid_uuid(response.Data.postCreated.id.Value);
      // print(is_valid);
      var cur_id = response.Data.postCreated.id.Value;
      // Assert.Is Type<string>(cur_id);
      Assert.IsInstanceOfType(cur_id, typeof(string));
      id_list.Add(int.Parse(cur_id));
      Assert.AreEqual("Mock Post", response.Data.postCreated.title.Value);
      Assert.AreEqual("This is a mock post.", response.Data.postCreated.content.Value);
      Console.WriteLine($"New data created: {response.Data}");
      flag_received_at_least_one_message = true;
    });

    Console.WriteLine("Subscribed to new posts.");

    //to wait and not close
    //This is not blocking
    //Therefore any prints from asynchronous handlers above will be printed in the console just fine
    // Console.ReadLine();

    bool are_incrementing(List<int> nums) {
      var prevs = Slice(nums, 0, -1);
      var nexts = Slice(nums, 1);
      return prevs.Zip(nexts, (prev, next) => prev < next).All(_ => _);
    }

    var all_good = await WaitForConditionAsync(
      condition: () => {
        return flag_received_at_least_one_message && id_list.Count >= 2 && are_incrementing(id_list);
      },
      timeout: TimeSpan.FromSeconds(10)
    );

    Assert.IsTrue(all_good);
  }

  /*
 In GraphQL, the Type Query and Type Mutation types have special significance and are indeed the starting points for any read or write operations, respectively.
 They are not just conventions; they are integral parts of the GraphQL schema definition.
  */
  [TestMethod]
  public async Task test12_conditionals() {
    var client = get_client("apollo12");

    string gqlString = await File.ReadAllTextAsync("gql_schemas/conditionals.gql");

    var q1 = new GraphQLRequest {
      Query = gqlString,
      Variables = new {
        userId = "ba03280b-19f0-4e6f-afd1-8e1cbfaba982",
        includePosts = false,
        skipEmail = false
      },
      OperationName = "GetUserProfileWithConditionals",
    };

    var r1 = await client.SendQueryAsync<dynamic>(q1);

    Assert.IsTrue(is_valid_uuid(r1.Data.user.id.Value));
    Assert.AreEqual("Hello World", r1.Data.user.name.Value);
    Assert.AreEqual("Hello World", r1.Data.user.email.Value);
    // Assert.Is Type<long>(r1.Data.user.age.Value);
    Assert.IsInstanceOfType(r1.Data.user.age.Value, typeof(long));
    Assert.IsFalse(r1.Data.user.ContainsKey("posts"));


    var q2 = new GraphQLRequest {
      Query = gqlString,
      Variables = new {
        userId = "ba03280b-19f0-4e6f-afd1-8e1cbfaba982",
        includePosts = false,
        skipEmail = true
      },
      OperationName = "GetUserProfileWithConditionals",
    };

    var r2 = await client.SendQueryAsync<dynamic>(q2);

    Assert.IsTrue(is_valid_uuid(r2.Data.user.id.Value));
    Assert.IsFalse(r2.Data.user.ContainsKey("email"));

    // Assert.Is Type<long>(r2.Data.user.age.Value);
    Assert.IsInstanceOfType(r2.Data.user.age.Value, typeof(long));

    Assert.IsFalse(r2.Data.user.ContainsKey("posts"));

    var q3 = new GraphQLRequest {
      Query = gqlString,
      Variables = new {
        userId = "ba03280b-19f0-4e6f-afd1-8e1cbfaba982",
        includePosts = true,
        skipEmail = false
      },
      OperationName = "GetUserProfileWithConditionals",
    };

    var r3 = await client.SendQueryAsync<dynamic>(q3);

    Assert.IsTrue(is_valid_uuid(r3.Data.user.id.Value));
    Assert.AreEqual("Hello World", r3.Data.user.email.Value);

    // Assert.Is Type<long>(r3.Data.user.age.Value);
    Assert.IsInstanceOfType(r3.Data.user.age.Value, typeof(long));

    Assert.IsTrue(r3.Data.user.ContainsKey("posts"));
    // not straightforward but casting does the trick here
    ((JArray)r3.Data.user.posts).ToList().ForEach(pp => {
      Assert.AreEqual("Hello World", pp["title"]);
    });

    var q4 = new GraphQLRequest {
      Query = gqlString,
      Variables = new {
        userId = "ba03280b-19f0-4e6f-afd1-8e1cbfaba982",
        includePosts = true,
        skipEmail = true
      },
      OperationName = "GetUserProfileWithConditionals",
    };

    var r4 = await client.SendQueryAsync<dynamic>(q4);

    Assert.IsTrue(is_valid_uuid(r4.Data.user.id.Value));
    Assert.IsFalse(r2.Data.user.ContainsKey("email"));

    // Assert.Is Type<long>(r4.Data.user.age.Value);
    Assert.IsInstanceOfType(r4.Data.user.age.Value, typeof(long));

    Assert.IsTrue(r4.Data.user.ContainsKey("posts"));
    // not straightforward but casting does the trick here
    ((JArray)r4.Data.user.posts).ToList().ForEach(pp => {
      Assert.AreEqual("Hello World", pp["title"]);
    });
  }

  [TestMethod]
  public async Task test11_fragment_filter_sort_paginate() {
    var client = get_client("apollo11");

    string gqlString = await File.ReadAllTextAsync("gql_schemas/filter_sort_pagination.gql");

    var qq = new GraphQLRequest {
      Query = gqlString,
      OperationName = "GetFilteredSortedPaginatedPosts",
    };

    var res = await client.SendQueryAsync<dynamic>(qq);

    Assert.AreEqual(2, res.Data.maposts.edges.Count);
    Assert.AreEqual(2, res.Data.maposts.edges[0].node.tags.Count);
  }

  [TestMethod]
  public async Task test10_fragment_alias() {
    var client = get_client("apollo07");

    //read gql string from contents of file "gql_schemas/fragments_aliases.gql"
    string gqlString = await File.ReadAllTextAsync("gql_schemas/fragments_aliases.gql");
    // print(gqlString);

    var qq = new GraphQLRequest {
      Query = gqlString,
      Variables = new {
        id1 = "ba03280b-19f0-4e6f-afd1-8e1cbfaba982",
        id2 = "dummy_id"
      }
    };

    var res = await client.SendQueryAsync<dynamic>(qq);

    Assert.IsTrue(is_valid_uuid(res.Data.user1.id.Value));
    Assert.AreEqual("Hello World", res.Data.user1.name.Value);
    Assert.AreEqual("Hello World", res.Data.user1.email.Value);
    Assert.AreEqual(2, res.Data.user1.posts.Count);
    Assert.AreEqual("Hello World", res.Data.user1.posts[0].title.Value);
    Assert.AreEqual(2, res.Data.user2.posts.Count);
    Assert.AreEqual("Hello World", res.Data.user2.posts[1].content.Value);
  }

  [TestMethod]
  public async Task test07_operation_name() {
    var client = get_client("apollo07");

    var qq = new GraphQLRequest {
      Query = @"
      query GetUserWithPosts($userId: ID!) {
        user(id: $userId) {
          id
          name
          email
          posts {
            id
            title
            content
          }
        }
      }

      query GetUserWPosts($userId: ID!) {
        user(id: $userId) {
          id
          name
          email
          posts {
            id
            title
            content
          }
        }
      }
      ",
      Variables = new {
        userId = "ba03280b-19f0-4e6f-afd1-8e1cbfaba982"
      }
    };

    var res = await client.SendQueryAsync<dynamic>(qq);


    //you CANNOT DO 2 queries at once, you need to specify the operation name
    var exception = Assert.ThrowsException<RuntimeBinderException>(() => {
      Console.WriteLine(res.Data.ToString());
    });

    // Assert. Contains("Cannot perform runtime binding on a null reference", exception.Message);
    StringAssert.Contains(exception.Message, "Cannot perform runtime binding on a null reference");

    //BUT you can do 1 query and 1 mutation

    var qm = new GraphQLRequest {
      Query = @"
      query GetUserWithPosts($userId: ID!) {
        user(id: $userId) {
          id
          name
          email
          posts {
            id
            title
            content
          }
        }
      }

      mutation SetPosts($postId: ID!) {
        updatePost(id: $postId, input: {
          content: ""Totally new content""
          isPublished: false
        }) {
            id
            title
            content
        }
      }
      ",
      Variables = new {
        userId = "ba03280b-19f0-4e6f-afd1-8e1cbfaba982",
        postId = "some-post-id"
      },
      OperationName = "SetPosts" //specifying the operation name works ok
    };

    var double_res = await client.SendQueryAsync<dynamic>(qm);

    Assert.IsTrue(is_valid_uuid(double_res.Data.updatePost.id.Value));
  }

  [TestMethod]
  public async Task test07_null_exception() {
    var client = get_client("apollo06");

    var qq = new GraphQLRequest {
      Query = @"
      query Query($postId: ID!) {
        post(id: $postId) {
          title
          content
          createdAt
          nonExistingAttribute
          user {
            email
          }
          tags {
            name
          }
        }
      }
      ",
      Variables = new {
        postId = "ba03280b-19f0-4e6f-afd1-8e1cbfaba982"
      }
    };

    var res = await client.SendQueryAsync<dynamic>(qq);

    //requires the import using Microsoft.CSharp.RuntimeBinder;
    var exception = Assert.ThrowsException<RuntimeBinderException>(() => {
      Assert.AreEqual("Hello World", res.Data.post.title.Value);
    });

    // Assert. Contains("Cannot perform runtime binding on a null reference", exception.Message);
    StringAssert.Contains(exception.Message, "Cannot perform runtime binding on a null reference");
  }

  [TestMethod]
  public async Task test06() {
    //- Case: Create a `Post` and associate it with a `User`. Update the post to associate it with a different user and verify the relationship changes correctly.
    var client = get_client("apollo06");

    var create_post_query = new GraphQLRequest {
      Query = @"
      mutation Mutation {
        createPost(input: {
          title: ""unknown day""
          content: ""fires in Evia""
          isPublished: false
          userId: ""6ba82b66-c463-4fee-86dd-0f00ec4de2b7""
        }) {
          id
          title
          content
          createdAt
          isPublished
          user {
            id
            name
            email
            posts {
              id
              title
            }
          }
        }
      }
      ",
    };

    var res = await client.SendQueryAsync<dynamic>(create_post_query);

    //in a non mocked scenario you would that one equals the other
    Assert.IsTrue(is_valid_uuid(res.Data.createPost.id.Value));
    Assert.AreEqual("Hello World", res.Data.createPost.title.Value);

    // var collection = ((IEnumerable<dynamic>)res.Data.createPost.user.posts).Select(pp => pp.title.Value);
    // Assert. Contains("Hello World", collection);
    var collection = ((IEnumerable<dynamic>)res.Data.createPost.user.posts).Select(pp => pp.title.Value).ToList();
    CollectionAssert.Contains(collection, "Hello World");

    // post id: "ba03280b-19f0-4e6f-afd1-8e1cbfaba982",
    // new user id: "4b7f7bbf-4f1e-4406-90c9-4876885bce35"

    //update post to associate it with a different user
    var update_post_query = new GraphQLRequest {
      Query = @"
      mutation Mutation {
        updatePost(
          id: ""ba03280b-19f0-4e6f-afd1-8e1cbfaba982""
          input: {
          userId: ""4b7f7bbf-4f1e-4406-90c9-4876885bce35""
        }) {
          user {
            id
            name
          }
        }
      }
      "
    };

    var up_res = await client.SendQueryAsync<dynamic>(update_post_query);

    Assert.IsTrue(is_valid_uuid(up_res.Data.updatePost.user.id.Value));
    Assert.AreEqual("Hello World", up_res.Data.updatePost.user.name.Value);
  }

  //dotnet test --filter "TestCategory=integration"
  [TestMethod]
  [TestCategory("integration")]
  [TestCategory("high_prio")]
  public async Task test05() {
    var client = get_client("apollo05");

    String name = "ma name";
    var email = "invalid email";
    var age = 0;

    var qq = new GraphQLRequest {
      //here the named query is "Query"
      Query = @"
      mutation Mutation($name: String!
      $email: String!
      $age: Int) {
        createUser(input: {
          name: $name
          email: $email
          age: $age
        }) {
          id
          name
          email
          age
        }
      }
      ",
      Variables = new {
        name,
        email,
        age
      }
    };

    var res = await client.SendQueryAsync<dynamic>(qq);

    Assert.IsTrue(is_valid_uuid(res.Data.createUser.id.Value));

    bool is_valid = is_valid_json_schema(res.Data, "schema_create_user.json", out List<string> errorMessages);

    errorMessages.ForEach(Console.WriteLine);

    Assert.IsTrue(is_valid, "Json Schema is not valid");
  }

  //dotnet test --filter FullyQualifiedName~TestGraphQL.test04_upd_assert_and_del
  //if you execute dotnet test --filter FullyQualifiedName~TestGraphQL.test04 then all tests starting with test04 will be executed
  [TestMethod]
  public async Task test04_upd_assert_and_del() {
    var client = get_client("apollo04");

    var updatePostId = "edea47a9-dfb8-4e0e-bcfe-3b1e6fece673";
    var query = @"
    mutation Mutation($updatePostId: ID!) {
      updatePost(id: $updatePostId, input: {
        title: ""Perfect day"",
        content: ""The stories we tell ourselves""
      }) {
        id
        title
        content
      }
    }";

    var req = new GraphQLRequest {
      Query = query,
      Variables = new {
        updatePostId = updatePostId
      }
    };


    var update_res = await client.SendQueryAsync<dynamic>(req);

    Assert.IsTrue(is_valid_uuid(update_res.Data.updatePost.id.Value));
    Assert.AreEqual("Hello World", update_res.Data.updatePost.title.Value);
    Assert.AreEqual("Hello World", update_res.Data.updatePost.content.Value);


    //get post again and assert

    var get_query = new GraphQLRequest {
      //here the named query is "Query"
      Query = @"
      query Query($id: ID!)
      {
        post(id: $id) {
          id
          title
          content
        }
      }
      ",
      Variables = new {
        id = updatePostId
      }
    };

    var get_res = await client.SendQueryAsync<dynamic>(get_query);

    Assert.IsTrue(is_valid_uuid(get_res.Data.post.id.Value));
    Assert.AreEqual("Hello World", get_res.Data.post.title.Value);
    Assert.AreEqual("Hello World", get_res.Data.post.content.Value);

    //delete post

    var del_query = new GraphQLRequest {
      //here the named query is "Query"
      Query = @"
      mutation Mutation($deletePostId: ID!)
      {
        deletePost(id: $deletePostId)
      }
      ",
      Variables = new {
        deletePostId = updatePostId
      }
    };

    var del_res = await client.SendQueryAsync<dynamic>(del_query);

    // Assert.Is Type<bool>(del_res.Data.deletePost.Value);
    Assert.IsInstanceOfType(del_res.Data.deletePost.Value, typeof(bool));
  }

  [TestMethod]
  public async Task test04_create_post_and_list_it() {
    var client = get_client("apollo04");

    var query = new GraphQLRequest {
      //here we could have "mutation" if we want to be unnamed 
      //or "mutation Mutation" if we want to specify that it is the mutation named "Mutation" (which is the default name)
      Query = @"
        mutation Mutation {
            createPost(input: {
            title: ""A Karystos day""
            content: ""A Pancake in Naftilos""
            isPublished: true
            tags: [""vacation"", ""greek""]
          }) {
              id
              title
              content
              createdAt
              views
              rating
              isPublished
              tags {
                id
                name
              }
            }
        }
        "
    };

    var response = await client.SendQueryAsync<dynamic>(query);

    Assert.AreEqual(2, response.Data.createPost.tags.Count);
    Assert.IsTrue(is_valid_uuid(response.Data.createPost.id.Value));
    Assert.AreEqual("Hello World", response.Data.createPost.title.Value);
    Assert.AreEqual("Hello World", response.Data.createPost.content.Value);
    Assert.AreEqual("Hello World", response.Data.createPost.createdAt.Value);

    // Assert.Is Type<long>(response.Data.createPost.views.Value);
    // Assert.Is Type<double>(response.Data.createPost.rating.Value);
    // Assert.Is Type<bool>(response.Data.createPost.isPublished.Value);
    Assert.IsInstanceOfType(response.Data.createPost.views.Value, typeof(long));
    Assert.IsInstanceOfType(response.Data.createPost.rating.Value, typeof(double));
    Assert.IsInstanceOfType(response.Data.createPost.isPublished.Value, typeof(bool));

    Assert.AreEqual("Hello World", response.Data.createPost.tags[0].name.Value);
    Assert.IsTrue(is_valid_uuid(response.Data.createPost.tags[1].id.Value));


    //find that post exists in all posts after creation

    var list_query = new GraphQLRequest {
      //here the named query is "Query"
      Query = @"
        query Query
        {
          posts {
            id
            title
          }
        }
        "
    };

    var list_res = await client.SendQueryAsync<dynamic>(list_query);
    Assert.AreEqual(2, list_res.Data.posts.Count);

    var posts = (IEnumerable<dynamic>)list_res.Data.posts;
    var collection = posts.Select(pp => pp.title.Value).ToList();

    // Assert.Contains("Hello World", collection);
    CollectionAssert.Contains(collection, "Hello World");
  }

  [TestMethod]
  public async Task test03() {
    var client = get_client("apollo03");

    var query = new GraphQLRequest {
      Query = @"
        query
        {
          posts(
            tag: ""mytag""
            sortBy: ""views""
            sortDirection: ""descending""
            limit: 3
            offset: 10) {
            id
            title
            content
            createdAt
            views
            rating
            isPublished
            tags {
              id
              name
            }
          }
        }
        "
    };

    var response = await client.SendQueryAsync<dynamic>(query);

    Assert.AreEqual(2, response.Data.posts.Count);

    Assert.IsTrue(is_valid_uuid(response.Data.posts[0].id.Value));

    Assert.AreEqual("Hello World", response.Data.posts[0].title.Value);
    Assert.AreEqual("Hello World", response.Data.posts[0].content.Value);
    Assert.AreEqual("Hello World", response.Data.posts[0].createdAt.Value);

    // Assert.Is Type<long>(response.Data.posts[0].views.Value);
    // Assert.Is Type<double>(response.Data.posts[0].rating.Value);
    // Assert.Is Type<bool>(response.Data.posts[0].isPublished.Value);
    Assert.IsInstanceOfType(response.Data.posts[0].views.Value, typeof(long));
    Assert.IsInstanceOfType(response.Data.posts[0].rating.Value, typeof(double));
    Assert.IsInstanceOfType(response.Data.posts[0].isPublished.Value, typeof(bool));

    Assert.AreEqual(2, response.Data.posts[1].tags.Count);
    Assert.AreEqual("Hello World", response.Data.posts[1].tags[1].name.Value);
    Assert.IsTrue(is_valid_uuid(response.Data.posts[1].tags[0].id.Value));
  }

  [TestMethod]
  public async Task test02() {
    var client = get_client("apollo02");

    var query = new GraphQLRequest {
      Query = @"
        query
        {
          user(id: 1) {
            id
            name
            email
            age
            height
            isActive
            posts {
              id
              title
              content
              createdAt
              views
              rating
              isPublished
              comments {
                id
                text
                createdAt
                likes
                author {
                  name
                  height
                }
              }
            }
          }
        }
        "
    };

    var response = await client.SendQueryAsync<dynamic>(query);

    Assert.IsTrue(is_valid_uuid(response.Data.user.id.Value));
    Assert.AreEqual("Hello World", response.Data.user.name.Value);
    Assert.AreEqual("Hello World", response.Data.user.email.Value);

    // Assert.Is Type<long>(response.Data.user.age.Value);
    // Assert.Is Type<double>(response.Data.user.height.Value);
    // Assert.Is Type<bool>(response.Data.user.isActive.Value);
    Assert.IsInstanceOfType(response.Data.user.age.Value, typeof(long));
    Assert.IsInstanceOfType(response.Data.user.height.Value, typeof(double));
    Assert.IsInstanceOfType(response.Data.user.isActive.Value, typeof(bool));

    //count that posts are 2
    Assert.AreEqual(2, response.Data.user.posts.Count);

    //assert content in first post
    Assert.IsTrue(is_valid_uuid(response.Data.user.posts[0].id.Value));
    Assert.AreEqual("Hello World", response.Data.user.posts[0].title.Value);
    Assert.AreEqual("Hello World", response.Data.user.posts[0].content.Value);
    Assert.AreEqual("Hello World", response.Data.user.posts[0].createdAt.Value);

    // Assert.Is Type<long>(response.Data.user.posts[0].views.Value);
    // Assert.Is Type<double>(response.Data.user.posts[0].rating.Value);
    Assert.IsInstanceOfType(response.Data.user.posts[0].views.Value, typeof(long));
    Assert.IsInstanceOfType(response.Data.user.posts[0].rating.Value, typeof(double));

    Assert.IsFalse(response.Data.user.posts[0].isPublished.Value);
    Assert.AreEqual(2, response.Data.user.posts[0].comments.Count);

    //assert 2nd comment of 2nd post
    Assert.IsTrue(is_valid_uuid(response.Data.user.posts[1].comments[1].id.Value));
    Assert.AreEqual("Hello World", response.Data.user.posts[1].comments[1].text.Value);
    Assert.AreEqual("Hello World", response.Data.user.posts[1].comments[1].createdAt.Value);

    // Assert.Is Type<long>(response.Data.user.posts[1].comments[1].likes.Value);
    Assert.IsInstanceOfType(response.Data.user.posts[1].comments[1].likes.Value, typeof(long));

    Assert.AreEqual("Hello World", response.Data.user.posts[1].comments[1].author.name.Value);

    // Assert.Is Type<double>(response.Data.user.posts[1].comments[1].author.height.Value);
    Assert.IsInstanceOfType(response.Data.user.posts[1].comments[1].author.height.Value, typeof(double));
  }

  //dotnet test --filter FullyQualifiedName~TestGraphQL.test01
  [TestMethod]
  public async Task test01() {
    var client = get_client("apollo01");

    var query = new GraphQLRequest {
      Query = @"
        query
        {
          user(id: 2) {
            id
            name
            email
            age
          }
        }"
    };

    var response = await client.SendQueryAsync<dynamic>(query);

    Assert.IsTrue(is_valid_uuid(response.Data.user.id.Value));
    Assert.AreEqual(response.Data.user.name.Value, "Hello World");
    Assert.AreEqual(response.Data.user.email.Value, "Hello World");

    // Assert.Is Type<long>(response.Data.user.age.Value);
    Assert.IsInstanceOfType(response.Data.user.age.Value, typeof(long));
  }

  //dotnet test --filter FullyQualifiedName~TestGraphQL.test_01
  [TestMethod]
  public async Task test00() {
    // var client = new GraphQLHttpClient("https://localhost:4000/", new NewtonsoftJsonSerializer());
    var client = get_client("apollo00");

    var query = new GraphQLRequest {
      Query = @"
      query {
        hello(target: ""baby"")
        name
      }"
    };

    var response = await client.SendQueryAsync<dynamic>(query);

    Assert.IsNotNull(response);
    //due to mock server:
    Assert.AreEqual(response.Data.hello.Value, "Hello World");
    Assert.AreEqual(response.Data.name.Value, "Hello World");
    // Console.WriteLine(response.Data.hello.GetType()); //Newtonsoft.Json.Linq.JValue
  }

  /*
    // Destructor
    ~HelloPlaywrightTest()
    {
      // Cleanup code

    }
    */
}