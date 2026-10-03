using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Butterfly : MonoBehaviour {

    Vector3 startPosition;
    Quaternion startRotation;
    Animator animator;
    float timer;
    BStates state;
    float height;
    ButterfliesArea butterfliesArea;
    List<Transform> playersAround = new List<Transform>();
    public enum BStates {
        Patrol, Sit, Landing, FlyAway
    }

    internal void Move()
    {
        timer -= Time.deltaTime;
        Vector3 pos;
        Vector3 newRight;
        switch (state)
        {
            case BStates.FlyAway:
                Vector3 runVector = Vector3.zero;
                foreach (var t in playersAround)
                    runVector += (t.transform.position - transform.position).normalized;
                runVector.Normalize();
                pos = transform.position;
                pos.y = Mathf.MoveTowards(pos.y, height, Time.deltaTime * butterfliesArea.speed);
                pos -= transform.forward * Time.deltaTime * butterfliesArea.speed / 2f;
                newRight = pos - startPosition;
                newRight.y = transform.right.y;
                transform.forward = Vector3.MoveTowards(transform.forward, runVector, Time.deltaTime * butterfliesArea.speed / 4f);
                transform.position = pos;
                break;

            case BStates.Landing:
                pos = transform.position;
                pos = Vector3.MoveTowards(pos, startPosition, Time.deltaTime* butterfliesArea.speed);
                transform.position = pos;
                transform.rotation = Quaternion.Lerp(transform.rotation, startRotation, Time.deltaTime * butterfliesArea.speed /2f);
                if (pos.Equals(startPosition))
                {
                    timer = UnityEngine.Random.Range(0, 10f);
                    state = BStates.Sit;
                    height = startPosition.y + UnityEngine.Random.Range(0f, butterfliesArea.maxHeight);
                }
                break;

            case BStates.Patrol:
                animator.SetInteger("State",1);
                pos = transform.position;
                pos.y = Mathf.MoveTowards(pos.y, height, Time.deltaTime * butterfliesArea.speed);
                pos -= transform.forward * Time.deltaTime* butterfliesArea.speed / 2f;
                newRight = pos - startPosition;
                newRight.y = 0f;
                transform.right = Vector3.MoveTowards(transform.right, newRight, Time.deltaTime * butterfliesArea.speed /4f);
                //transform.up = Vector3.MoveTowards(transform.up, Vector3.up, Time.deltaTime * butterfliesArea.speed / 4f);
                transform.position = pos;
                if (pos.y == height)
                    height = startPosition.y + UnityEngine.Random.Range(0f, butterfliesArea.maxHeight);
                if (timer < 0f)
                {
                    timer = UnityEngine.Random.Range(0, 10f);
                    state = (BStates)UnityEngine.Random.Range(0, 3);
                    int r = UnityEngine.Random.Range(0, 3);
                    height = startPosition.y + UnityEngine.Random.Range(0f, butterfliesArea.maxHeight);
                    if (state == BStates.Sit)
                    {
                        state = BStates.Landing;
                    }
                }
                break;
            case BStates.Sit:
                animator.SetInteger("State", 0);
                if (timer < 0f)
                {
                    timer = UnityEngine.Random.Range(0, 10f);
                    state = (BStates)UnityEngine.Random.Range(0, 2);
                    if (state != BStates.Sit)
                    {
                        height = startPosition.y + UnityEngine.Random.Range(0f, butterfliesArea.maxHeight);
                    }
                }
                break;
        }
    }

    internal void Initialize(ButterfliesArea butterfliesArea)
    {
        startPosition = transform.position;
        startRotation = transform.rotation;
        height = startPosition.y;
        this.butterfliesArea = butterfliesArea;
        state = BStates.Sit;
        timer = UnityEngine.Random.Range(0, 10f);
        animator = GetComponent<Animator>();
        AnimatorStateInfo animatorState = animator.GetCurrentAnimatorStateInfo(0);//could replace 0 by any other animation layer index
        animator.Play(animatorState.fullPathHash, -1, UnityEngine.Random.Range(0f, 1f));
    }

    public void AddPlayer(Transform t)
    {
        playersAround.Add(t);
    }

    public void RemovePlayer(Transform t)
    {
        playersAround.Remove(t);
    }

    public void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            AddPlayer(other.transform);
            state = BStates.FlyAway;
            height = startPosition.y + UnityEngine.Random.Range(0f, butterfliesArea.maxHeight);
            animator.SetInteger("State", 1);
        }
    }

    public void OnTriggerExit(Collider other)
    {
        if (other.tag == "Player")
        {
            RemovePlayer(other.transform);
            if (playersAround.Count == 0)
                state = BStates.Patrol;
        }
    }
}
