package lbapiaws

import (
	"context"
	"fmt"
	"log"
	"math/rand"
	"time"

	"github.com/aws/aws-sdk-go-v2/feature/dynamodb/attributevalue"
	"github.com/aws/aws-sdk-go-v2/feature/dynamodb/expression"
	"github.com/aws/aws-sdk-go-v2/service/dynamodb"
	"github.com/aws/aws-sdk-go-v2/service/dynamodb/types"
	"github.com/aws/aws-sdk-go/aws"
)

var (
	timeoutWindow = 3 * time.Second
)

// Adds the player's submitted hand from the client to the Database.
//
// BUG: Need to check that types match, and contents are not null before marshalling.
func (ddbh dDBHandler) doAddHand(h handInfo) error {
	item, err := attributevalue.MarshalMap(h)
	if err != nil {
		log.Panicf("unable to marshal submitted hand: %v", err)
	}

	_, err = ddbh.DynamoDbClient.PutItem(context.TODO(), &dynamodb.PutItemInput{
		TableName: aws.String(ddbh.TableName),
		Item:      item,
	})
	if err != nil {
		log.Printf("couldn't add item to table: %v\n", err)
	}

	return err
}

func (ddbh dDBHandler) addHand(h handInfo) error {
	err := make(chan error, 1)

	go func() {
		err <- ddbh.doAddHand(h)
	}()
	select {
	case <-time.After(timeoutWindow):
		return fmt.Errorf("timeout - could not add to playerHands table in allotted window")

	case err := <-err:
		if err != nil {
			return fmt.Errorf("addHand execution failed. error: %v", err)
		}
		return nil
	}
}

func (ddbh dDBHandler) doQueryHands(version string) queryHandsResult {
	var availableHands []handInfo
	var response *dynamodb.QueryOutput

	keyEx := expression.Key("version").Equal(expression.Value(version))
	expr, err := expression.NewBuilder().WithKeyCondition(keyEx).Build()
	if err != nil {
		return queryHandsResult{
			nil,
			fmt.Errorf("could not build expression for query. error: %v", err),
		}
	} else {
		response, err = ddbh.DynamoDbClient.Query(context.TODO(), &dynamodb.QueryInput{
			TableName:                 aws.String(ddbh.TableName),
			ExpressionAttributeNames:  expr.Names(),
			ExpressionAttributeValues: expr.Values(),
			KeyConditionExpression:    expr.KeyCondition(),
		})
		if err != nil {
			return queryHandsResult{
				nil,
				fmt.Errorf("could not query for playerHands in v%v. error: %v", version, err),
			}
		} else {
			err = attributevalue.UnmarshalListOfMaps(response.Items, &availableHands)
			if err != nil {
				return queryHandsResult{
					availableHands,
					fmt.Errorf("couldn't unmarshal query response. error: %v", err),
				}
			}
		}
	}

	return queryHandsResult{availableHands, nil}
}

// Queries *all* entries in the database by version number.
//
// NOTE: This operation is gonna be expensive for a Lambda later on, so this result will eventually
// need to be cached later.
func (ddbh dDBHandler) queryHands(version string) ([]handInfo, error) {
	result := make(chan queryHandsResult, 1)

	go func() {
		result <- ddbh.doQueryHands(version)
	}()
	select {
	case <-time.After(timeoutWindow):
		return nil, fmt.Errorf("timeout - could not query for playerHands in allotted window")

	case result := <-result:
		return result.HandInfoSlice, nil
	}

}

// Creates the composite key for the playerHand dynamodb Table
//
// Use this function if you need to specifically target a player in the database
func (h handInfo) GetKey() (map[string]types.AttributeValue, error) {
	version, err := attributevalue.Marshal(h.Version)
	if err != nil {
		return nil, fmt.Errorf("unable to marshal attribute 'version' with value %v, error: %v", h.Version, err)
	}
	playerId, err := attributevalue.Marshal(h.PlayerId)
	if err != nil {
		return nil, fmt.Errorf("unable to marshal attribute 'playerId' with value %v, error: %v", h.PlayerId, err)
	}
	return map[string]types.AttributeValue{"version": version, "playerId": playerId}, nil
}

// Selects a random entry in []handInfo
//
// It ain't matchmaking, but it's honest work
func (ddbh dDBHandler) chooseHand(h []handInfo) handInfo {
	rand.Seed(time.Now().Unix())
	selectedHand := h[rand.Intn(len(h))]
	return selectedHand
}
